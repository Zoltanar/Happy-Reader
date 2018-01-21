using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
    /// <summary>
    /// Class for establishing connection with VNDB API and interacting with it.
    /// </summary>
    public partial class VndbConnection
    {
        public VndbConnection([NotNull]Action<string,MessageSeverity> textAction, Action<string,bool> advancedModeAction, Action refreshListAction, Action<APIStatus> changeStatusAction = null)
        {
            TextAction = textAction;
            _advancedAction = advancedModeAction;
            _refreshListAction = refreshListAction;
            if (changeStatusAction != null)
            {
                _changeStatusAction = status =>
                {
                    //runs the mandatory part of changeApiStatus first, then runs provided action
                    ChangeAPIStatus(status);
                    changeStatusAction(status);
                };
            }
            else _changeStatusAction = ChangeAPIStatus;
        }

        public const int VndbAPIMaxYear = 99999999;
        private const string VndbHost = "api.vndb.org";
        private const ushort VndbPort = 19534;
        private const ushort VndbPortTLS = 19535;
        private const byte EndOfStreamByte = 0x04;
        private Stream _stream;
        private TcpClient _tcpClient;
        public Response LastResponse;
        public LogInStatus LogIn = LogInStatus.No;
        public APIStatus Status = APIStatus.Closed;

        /// <summary>
        /// Open stream with VNDB API.
        /// </summary>
        private void Open(bool printCertificates)
        {
            LogToFile($"Attempting to open connection to {VndbHost}:{VndbPortTLS}");
            var complete = false;
            var retries = 0;
            var certs = new X509CertificateCollection();
            var certFiles = Directory.GetFiles("Program Data\\Certificates");
            foreach (var certFile in certFiles) certs.Add(X509Certificate.CreateFromCertFile(certFile));
            if (printCertificates)
            {
                LogToFile("Local Certificate data - subject/issuer/format/effectivedate/expirationdate");
                foreach (var cert in certs) LogToFile($"{cert.Subject}\t{cert.Issuer}\t{cert.GetFormat()}\t{cert.GetEffectiveDateString()}\t{cert.GetExpirationDateString()}");
            }
            while (!complete && retries < 5)
            {
                try
                {
                    retries++;
                    LogToFile($"Trying for the {retries}'th time...");
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(VndbHost, VndbPortTLS);
                    LogToFile("TCP Client connection made...");
                    var sslStream = new SslStream(_tcpClient.GetStream());
                    LogToFile("SSL Stream received...");
                    sslStream.AuthenticateAsClient(VndbHost, certs, SslProtocols.Tls12, true);
                    LogToFile("SSL Stream authenticated...");
                    if (sslStream.RemoteCertificate != null)
                    {
                        var subject = sslStream.RemoteCertificate.Subject;
                        if (printCertificates)
                        {
                            LogToFile("Remote Certificate data - subject/issuer/format/effectivedate/expirationdate");
                            LogToFile(subject + "\t - \t" + sslStream.RemoteCertificate.Issuer + "\t - \t" +
                                      sslStream.RemoteCertificate.GetFormat() + "\t - \t" +
                                      sslStream.RemoteCertificate.GetEffectiveDateString() + "\t - \t" +
                                      sslStream.RemoteCertificate.GetExpirationDateString());
                        }
                        if (!subject.Substring(3).Equals(VndbHost))
                        {
                            LogToFile(
                                $"Certificate received isn't for {VndbHost} so connection is closed (it was for {subject.Substring(3)})");
                            Status = APIStatus.Error;
                            return;
                        }
                    }

                    _stream = sslStream;
                    complete = true;
                    LogToFile($"Connected after {retries} tries.");
                }
                catch (SocketException e)
                {
                    LogToFile(e, "Conn Socket Error");
                    if (e.InnerException == null) continue;
                    LogToFile(e, "Conn Socket Error - Inner");
                    Thread.Sleep(1000);
                }
                catch (IOException e)
                {
                    LogToFile( e, "Conn Open Error");
                }
                catch (AuthenticationException e)
                {
                    LogToFile(e,"Conn Authentication Error");
                    if (e.InnerException == null) continue;
                    LogToFile(e,"Conn Authentication Error - Inner");
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
                {
                    LogToFile(ex,"Conn Other Error");
                }
            }
            if (_stream != null && _stream.CanRead) return;
            LogToFile($"Failed to connect after {retries} tries.");
            Status = APIStatus.Error;
            AskForNonSsl();
        }

        private void AskForNonSsl()
        {
            var messageResult = System.Windows.Forms.MessageBox.Show(@"Connection to VNDB failed, do you wish to try without SSL?",
                @"Connection Failed", System.Windows.Forms.MessageBoxButtons.YesNo);
            if (messageResult != System.Windows.Forms.DialogResult.Yes) return;
            LogToFile($"Attempting to open connection to {VndbHost}:{VndbPort} without SSL");
            Status = APIStatus.Closed;
            var complete = false;
            var retries = 0;
            while (!complete && retries < 5)
            {
                try
                {
                    retries++;
                    LogToFile($"Trying for the {retries}'th time...");
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(VndbHost, VndbPort);
                    LogToFile("TCP Client connection made...");
                    _stream = _tcpClient.GetStream();
                    LogToFile("Stream received...");
                    LogToFile($"Connected after {retries} tries.");
                    complete = true;
                }
                catch (IOException e)
                {
                    LogToFile(e,"Conn Open Error");
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
                {
                    LogToFile(ex, "Conn Other Error");
                }
                catch (Exception otherXException)
                {
                    LogToFile(otherXException, "Conn Other2 Error");
                }
            }
            if (_stream != null && _stream.CanRead) return;
            LogToFile($"Failed to connect after {retries} tries.");
            Status = APIStatus.Error;
        }

        /// <summary>
        /// Log into VNDB API, optionally using username/password.
        /// </summary>
        /// <param name="clientName">Name of Client accessing VNDB API</param>
        /// <param name="clientVersion">Version of Client accessing VNDB API</param>
        /// <param name="username">Username of user to log in as</param>
        /// <param name="password">Password of user to log in as</param>
        /// <param name="printCertificates">Default is true, logs certificates and prints to debug</param>
        public void Login(string clientName, string clientVersion, string username = null, char[] password = null, bool printCertificates = true)
        {
            if (Status != APIStatus.Closed) Close();
            Open(printCertificates);
            string loginBuffer;

            if (username != null && password != null)
            {
                loginBuffer =
                    $"login {{\"protocol\":1,\"client\":\"{clientName}\",\"clientver\":\"{clientVersion}\",\"username\":\"{username}\",\"password\":\"{new string(password)}\"}}";
                Query(loginBuffer);
                if (LastResponse.Type == ResponseType.Ok)
                {
                    LogIn = LogInStatus.YesWithPassword;
                    Status = APIStatus.Ready;
                }
            }
            else
            {
                loginBuffer = $"login {{\"protocol\":1,\"client\":\"{clientName}\",\"clientver\":\"{clientVersion}\"}}";
                Query(loginBuffer);
                if (LastResponse.Type == ResponseType.Ok)
                {
                    LogIn = LogInStatus.Yes;
                    Status = APIStatus.Ready;
                }
            }
        }

        private void Query(string command)
        {
            if (Status == APIStatus.Error) return;
            if(GSettings.AdvancedMode) _advancedAction?.Invoke(command, true);
            Status = APIStatus.Busy;
            byte[] encoded = Encoding.UTF8.GetBytes(command);
            var requestBuffer = new byte[encoded.Length + 1];
            Buffer.BlockCopy(encoded, 0, requestBuffer, 0, encoded.Length);
            requestBuffer[encoded.Length] = EndOfStreamByte;
            _stream.Write(requestBuffer, 0, requestBuffer.Length);
            var responseBuffer = new byte[4096];
            var totalRead = 0;
            while (true)
            {
                var currentRead = _stream.Read(responseBuffer, totalRead, responseBuffer.Length - totalRead);
                if (currentRead == 0) throw new Exception("Connection closed while reading login response");
                totalRead += currentRead;
                if (IsCompleteMessage(responseBuffer, totalRead)) break;
                if (totalRead != responseBuffer.Length) continue;
                var biggerBadderBuffer = new byte[responseBuffer.Length * 2];
                Buffer.BlockCopy(responseBuffer, 0, biggerBadderBuffer, 0, responseBuffer.Length);
                responseBuffer = biggerBadderBuffer;
            }
            LastResponse = Parse(responseBuffer, totalRead);
            if (GSettings.AdvancedMode) _advancedAction?.Invoke(LastResponse.JsonPayload, false);
            SetStatusFromLastResponseType();
        }

        private async Task QueryAsync(string query)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(query);
            var requestBuffer = new byte[encoded.Length + 1];
            Buffer.BlockCopy(encoded, 0, requestBuffer, 0, encoded.Length);
            requestBuffer[encoded.Length] = EndOfStreamByte;
            await _stream.WriteAsync(requestBuffer, 0, requestBuffer.Length);
            var responseBuffer = new byte[4096];
            var totalRead = 0;
            while (true)
            {
                var currentRead = await _stream.ReadAsync(responseBuffer, totalRead, responseBuffer.Length - totalRead);
                if (currentRead == 0) throw new Exception("Connection closed while reading login response");
                totalRead += currentRead;
                if (IsCompleteMessage(responseBuffer, totalRead)) break;
                if (totalRead != responseBuffer.Length) continue;
                var biggerBadderBuffer = new byte[responseBuffer.Length * 2];
                Buffer.BlockCopy(responseBuffer, 0, biggerBadderBuffer, 0, responseBuffer.Length);
                responseBuffer = biggerBadderBuffer;
            }
            LastResponse = Parse(responseBuffer, totalRead);
            SetStatusFromLastResponseType();
        }

        private void SetStatusFromLastResponseType()
        {
            switch (LastResponse.Type)
            {
                case ResponseType.Ok:
                case ResponseType.Results:
                case ResponseType.DBStats:
                    Status = APIStatus.Ready;
                    break;
                case ResponseType.Error:
                    Status = LastResponse.Error.ID.Equals("throttled") ? APIStatus.Throttled : APIStatus.Ready;
                    break;
                case ResponseType.Unknown:
                    Status = APIStatus.Error;
                    break;
            }
        }



        /// <summary>
        /// Close connection with VNDB API
        /// </summary>
        public void Close()
        {
            try
            {
                _tcpClient.GetStream().Close();
                _tcpClient.Close();
            }
            catch (ObjectDisposedException e)
            {
                LogToFile("Failed to close connection.");
                LogToFile(e.Message);
                LogToFile(e.StackTrace);
            }
            Status = APIStatus.Closed;
        }


        private static bool IsCompleteMessage(byte[] message, int bytesUsed)
        {
            if (bytesUsed == 0)
            {
                throw new Exception("You have a bug, dummy. You should have at least one byte here.");
            }

            // ASSUMPTION: simple request-response protocol, so we should see at most one message in a given byte array.
            // So, there's no need to walk the whole array looking for validity - just be lazy and check the last byte for EOS.
            return message[bytesUsed - 1] == EndOfStreamByte;
        }

        private static Response Parse(byte[] message, int bytesUsed)
        {
            if (!IsCompleteMessage(message, bytesUsed))
            {
                throw new Exception("You have a bug, dummy.");
            }

            var stringifiedResponse = Encoding.UTF8.GetString(message, 0, bytesUsed - 1);
            var firstSpace = stringifiedResponse.IndexOf(' ');
            var firstWord = firstSpace != -1 ? stringifiedResponse.Substring(0, firstSpace) : stringifiedResponse;
            var payload = firstSpace > 0 ? stringifiedResponse.Substring(firstSpace) : "";
            if (firstSpace == bytesUsed - 1)
            {
                // protocol violation!
                throw new Exception("Protocol violation: last character in response is first space");
            }
            switch (firstWord)
            {
                case "ok":
                    return new Response(ResponseType.Ok, payload);
                case "results":
                    return new Response(ResponseType.Results, payload);
                case "dbstats":
                    return new Response(ResponseType.DBStats, payload);
                case "error":
                    return new Response(ResponseType.Error, payload);
                default:
                    return new Response(ResponseType.Unknown, payload);
            }
        }

        public enum LogInStatus
        {
            No,
            Yes,
            YesWithPassword
        }

        public enum APIStatus
        {
            Ready,
            Busy,
            Throttled,
            Error,
            Closed
        }
    }

    /// <summary>
    /// Holds API's response to commands.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// If response is of type 'error', holds ErrorResponse
        /// </summary>
        public readonly ErrorResponse Error;
        /// <summary>
        /// Response in JSON format
        /// </summary>
        public readonly string JsonPayload;
        /// <summary>
        /// Type of response
        /// </summary>
        public readonly ResponseType Type;

        /// <summary>
        /// Constructor for Response
        /// </summary>
        /// <param name="type">Type of response</param>
        /// <param name="jsonPayload">Response in JSON format</param>
        public Response(ResponseType type, string jsonPayload)
        {
            Type = type;
            JsonPayload = jsonPayload;
            if (type == ResponseType.Error) Error = JsonConvert.DeserializeObject<ErrorResponse>(jsonPayload);
        }
    }


    /// <summary>
    /// Type of API Response
    /// </summary>
    public enum ResponseType
    {
        /// <summary>
        /// Returned by login command
        /// </summary>
        Ok,
        /// <summary>
        /// Returned by get commands 
        /// </summary>
        Results,
        /// <summary>
        /// Returned by dbstats command
        /// </summary>
        DBStats,
        /// <summary>
        /// Returned when there is an error
        /// </summary>
        Error,
        /// <summary>
        /// Returned in all other cases
        /// </summary>
        Unknown
    }
}