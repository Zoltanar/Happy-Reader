using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRGoogleTranslate
{
    public class Translation
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public DateTime Timestamp { get; set; }

        public Translation(string input, string output)
        {
            Input = input;
            Output = output;
            Timestamp = DateTime.UtcNow;
        }

        public Translation() { }
    }
}