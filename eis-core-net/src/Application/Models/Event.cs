namespace EisCore.Application.Models
{
    public class Event
    {
        public string Id {get;set;}
        public string Code {get;set;}        
        public string Name {get;set;}
        public string Description {get;set;}

        public Event()
        {
            
        }

        public Event(string id, string code, string name, string description)        
        {
            this.Id=id;
            this.Code=code;
            this.Name=name;
            this.Description=description;
            
        }
    }
}