using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EisCore.Domain.Entities
{

    public class Payload
    {

        public List<ChangedAttribute> ChangedAttributes{get;set;}
        public object Content { get; set; }

        public Payload()
        {
        }
        public Payload(object content)
        {
            this.Content = content;
        }
        public TOutput ConvertContent<TOutput>(){
            return JsonSerializer.Deserialize<TOutput>(this.Content.ToString());

        }

    }
}