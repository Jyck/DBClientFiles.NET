﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBClientFiles.NET.Internals.Segments;

namespace DBClientFiles.NET.Internals.Versions.Headers
{
    internal static class HeaderFactory
    {
        public static IFileHeader ReadHeader(Signatures signature, Stream dataStream)
        {
            using (var binaryReader = new BinaryReader(dataStream, Encoding.UTF8, true))
            {
                switch (signature)
                {
                    case Signatures.WDBC: return new WDBC(binaryReader);
                    case Signatures.WDB2: return new WDB2(binaryReader);
                    case Signatures.WDB3:
                    case Signatures.WDB4:
                        break;
                    case Signatures.WDB5: return new WDB5(binaryReader);
                    // TODO FIXME: I was lazy.
                    case Signatures.WDB6: return new WDB6(binaryReader);
                    case Signatures.WDC1: return new WDC1(binaryReader);
                    case Signatures.WDC2: return new WDC2(binaryReader);

                }
            }

            return null;
        }
    }
}