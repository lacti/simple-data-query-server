using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using DataQueryServer;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var gen = new MethodGenerator();
            /*
            var parser = gen.GetOrGenerateCsvParser(typeof(ItemData));
            var data = (ItemData)parser(new[] { "1234", "393939393", "4343"});
            Console.WriteLine("{0} - {1} - {2}", data.Timestamp, data.DbId, data.Name);

            var writer = gen.GetOrGenerateCsvWriter(typeof (ItemData));
            Console.WriteLine(writer(data));
            */

            IQueryable<ItemData> items;


            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                var m = gen.GetOrGenerateBinWriter(typeof (ItemData));
                m(writer, new ItemData { Timestamp = 1235, DbId = 333, Name = "abc"});

                var array = stream.ToArray();
                var m2 = gen.GetOrGenerateBinParser(typeof (ItemData));
                var offset = 0;
                var data2 = (ItemData)m2(array, ref offset);

                Console.WriteLine(BitConverter.ToString(stream.ToArray()));
            }
            /*
            var method = new DynamicMethod("CreateObj", typeof(IData), new[] { typeof(string[]) });
            var generator = method.GetILGenerator();
            var localVar = generator.DeclareLocal(typeof(ItemData));
            generator.Emit(OpCodes.Ldloca_S, localVar);
            generator.Emit(OpCodes.Initobj, typeof(ItemData));
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Box, typeof(ItemData));
            generator.Emit(OpCodes.Ret);
            var d = (Dele)method.CreateDelegate(typeof (Dele));
            var obj = d(new string[0]);
             */
        }

        private delegate IData Dele(string[] sts);
    }

    public struct ItemData : IData
    {
        public int Timestamp;
        public long DbId;
        public string Name;
    }
}
