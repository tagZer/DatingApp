using System;

namespace DatingApp.Api.Dtos
{
    // data we want to display from main User model in the list of users
    public class UserForListDto
    {
        public int Id { get; set; }
        public string Username { get; set; }     
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Alias { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastActive { get; set; }      
        public string City { get; set; }
        public string Country { get; set; }
        public string PhotoUrl { get; set; }
    }
}