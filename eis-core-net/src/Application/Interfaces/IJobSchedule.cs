using System;

namespace EisCore.Application.Interfaces
{
  
    public interface IJobSchedule
    {
        Type JobType { get; }

        string GetCronExpression();
    }
}