using System;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable UnusedMember.Global

namespace HRGoogleTranslate
{

    public class GoogleTranslation
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public DateTime Timestamp { get; set; }

        public GoogleTranslation(string input, string output)
        {
            Input = input;
            Output = output;
            Timestamp = DateTime.UtcNow;
        }
        
        public GoogleTranslation() { }
    }
}