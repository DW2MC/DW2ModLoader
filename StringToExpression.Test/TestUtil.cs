using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringToExpression.Test
{
    public static class TestUtil
    {
        public static string Highlight(this Substring segment, string startHighlight = "[", string endHighlight = "]")
        {
            return segment.Source
                .Insert(segment.End, endHighlight)
                .Insert(segment.Start, startHighlight);
        }
    }
}
