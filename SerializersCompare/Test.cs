﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSerializers
{
    class Test
    {
        private SimpleEntity originalObject;
        private SimpleEntity testObject;
        public void LoadEntityObject()
        {
            originalObject = new SimpleEntity();
            originalObject.FillDummyData();
            testObject = new SimpleEntity();
        }

        public void RunTests()
        {
            int loopLimit = 10000;
            List<Results> resultTable = new List<Results>();
            // JSON test
            resultTable.Add(TestSerializerInLoop(new JsonNET(), loopLimit));

            // BinaryFormatter test
            resultTable.Add(TestSerializerInLoop(new BinFormatter(), loopLimit));

            // ProtoBuf test
            resultTable.Add(TestSerializerInLoop(new ProtoBuf(), loopLimit));

            PrintResultTable(resultTable);

            return;
        }

        private void PrintResultTable(List<Results> resultTable)
        {
            // Stores as lists top-down but console printing is left to right!
            // This is inherently ugly, can be fixed if so desired
            int col1 = 5;
            int colN = 20;

            // Header - line 1
            string formatString = String.Format("{{0,{0}}}", col1);
            string toPrint = "";
            System.Console.Write(formatString, toPrint);
            for (int i = 0; i < resultTable.Count; i++)
            {
                toPrint = String.Format("{0}", resultTable[i].serName);
                formatString = String.Format("{{0,{0}}}", colN );
                System.Console.Write(formatString, toPrint);
            }
            System.Console.Write(Environment.NewLine);

            // Header - line 2
            formatString = String.Format("{{0,{0}}}", col1);
            toPrint = "Loop";
            System.Console.Write(formatString, toPrint);
            for (int i = 0; i < resultTable.Count; i++)
            {
                toPrint = String.Format("Size:{0} bytes", resultTable[i].sizeBytes);
                formatString = String.Format("{{0,{0}}}", colN );
                System.Console.Write(formatString, toPrint);
            }
            System.Console.Write(Environment.NewLine);

            // Rest of Table
            //Assumes: all colums have same length and rows line-up for same loop count
            for (int row = 0; row < resultTable[0].resultColumn.Count; row++)
            {
                formatString = String.Format("{{0,{0}}}", col1);
                toPrint = resultTable[0].resultColumn[row].iteration.ToString();
                System.Console.Write(formatString, toPrint);

                for (int i = 0; i < resultTable.Count; i++)
                {
                    formatString = String.Format("{{0,{0}}}", colN );
                    toPrint = String.Format("{0,4:n4} ms", resultTable[i].resultColumn[row].time.TotalMilliseconds);
                    System.Console.Write(formatString, toPrint);
                }
                System.Console.Write(Environment.NewLine);
            }
        }

        private Results TestSerializerInLoop(dynamic ser, int loopLimit)
        {
            
            // geometrically scale at x2
            Results result = new Results();
            result.serName = ser.GetName();


            result.resultColumn = new List<ResultColumnEntry>();
            for (int i = 1; i <= loopLimit; i = i * 2)
            {
                int sizeInBytes;
                bool success;
                // test at this loop count
                ResultColumnEntry resultEntry = TestSerializer(ser, i, out sizeInBytes, out success);
                result.resultColumn.Add(resultEntry);
                result.sizeBytes = sizeInBytes;
                result.success = success;
            }

            return result;
        }

        private ResultColumnEntry TestSerializer(dynamic ser, int iterations, out int sizeInBytes, out bool success)
        {
            Stopwatch sw = new Stopwatch();
            
            // BINARY serializers 
            // eg: ProtoBufs, Bin Formatter etc
            if (ser.IsBinary()) 
            {
                byte[] binOutput;
                sw.Reset();
                sw.Start();
                for (int i = 0; i < iterations; i++)
                {
                    binOutput = ser.Serialize(originalObject);
                    testObject = ser.Deserialize<SimpleEntity>(binOutput);
                }
                sw.Stop();
                // Find size outside loop to avoid timing hits
                binOutput = ser.Serialize(originalObject);
                sizeInBytes = binOutput.Count();
            }
            // TEXT serializers
            // eg. JSON, XML etc
            else 
            {
                string strOutput;
                sw.Reset();
                sw.Start();
                for (int i = 0; i < iterations; i++)
                {
                    strOutput = ser.Serialize(originalObject);
                    testObject = ser.Deserialize<SimpleEntity>(strOutput);
                }
                sw.Stop();

                // Find size outside loop to avoid timing hits
                // Size as bytes for UTF-8 as it's most common on internet
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] strInBytes = encoding.GetBytes(ser.Serialize(originalObject));
                sizeInBytes = strInBytes.Count();
            }
            ResultColumnEntry entry = new ResultColumnEntry();
            entry.iteration = iterations;
            long avgTicks = sw.Elapsed.Ticks / iterations;
            if (avgTicks == 0)
            {
                // sometime when running windows inside a VM this is 0! Possible vm issue?
                Debugger.Break();
            }
            entry.time = new TimeSpan(avgTicks);


            // Debug: To aid printing to screen, human debugging etc. Json used as best for console presentation
            JsonNET jsonSer = new JsonNET();

            string orignalObjectAsJson = JsonHelper.FormatJson(jsonSer.Serialize(originalObject));
            string testObjectAsJson = JsonHelper.FormatJson(jsonSer.Serialize(testObject));
            success = true;
            if (orignalObjectAsJson != testObjectAsJson)
            {
                System.Console.WriteLine(">>>> {0} FAILED <<<<", ser.GetName());
                System.Console.WriteLine("\tOriginal and regenerated objects differ !!");
                System.Console.WriteLine("\tRegenerated objects is:");
                System.Console.WriteLine(testObjectAsJson);
                success = false;
            }

            return entry;
        }
    }
}
