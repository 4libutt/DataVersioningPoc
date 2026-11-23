using System;
using System.Collections.Generic;
using System.Text;

namespace DataVersioningPoc.Models
{ 
    [DataStructureVersion(1)]
    public class Patient
    {
        public DateTime BirthDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        //public string PhoneNumber { get; set; } = string.Empty;

    }
}
