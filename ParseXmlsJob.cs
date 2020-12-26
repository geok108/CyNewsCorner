using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner
{
    public class ParseXmlsJob : IJob
    {
        private readonly CyNewsCornerContext Db;

        public async Task Execute(IJobExecutionContext context)
        {
            //Db.Sources.Add(new DataModels.NewsSource{Name = "Test" });
		    await Console.Out.WriteLineAsync("Greetings from HelloJob!");
        }
    }
}
