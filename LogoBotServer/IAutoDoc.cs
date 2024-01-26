using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogoBotServer
{
    internal interface IAutoDoc
    {
        List <string> getOnlyDocByLpSN(List<string> listOfLpSn);
        string getAllDocByLpSN(List<string> listOfLpSn);

    }
}
