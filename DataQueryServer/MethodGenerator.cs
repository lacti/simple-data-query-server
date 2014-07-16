using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DataQueryServer
{
    public class MethodGenerator
    {
        private readonly Dictionary<string, CsvParser> _csvParsers = new Dictionary<string, CsvParser>();
        private readonly Dictionary<string, CsvWriter> _csvWriters = new Dictionary<string, CsvWriter>();
        private readonly Dictionary<string, BinParser> _binParsers = new Dictionary<string, BinParser>();
        private readonly Dictionary<string, BinWriter> _binWriters = new Dictionary<string, BinWriter>();

        public delegate IData CsvParser(string[] parts);
        public delegate string CsvWriter(IData data);
        public delegate IData BinParser(byte[] bytes, ref int offset);
        public delegate void BinWriter(BinaryWriter writer, IData data);

        public CsvParser GetOrGenerateCsvParser(Type valueType)
        {
            var methodName = string.Format("Parse{0}FromCsv", valueType.Name);
            if (_csvParsers.ContainsKey(methodName))
                return _csvParsers[methodName];

            var method = new DynamicMethod(methodName, typeof(IData), new[] { typeof(string[]) });
            var generator = method.GetILGenerator();
            var localVar = generator.DeclareLocal(valueType);
            generator.Emit(OpCodes.Ldloca_S, localVar);
            generator.Emit(OpCodes.Initobj, valueType);
            var arrayIndex = 0;
            foreach (var field in valueType.GetFields())
            {
                generator.Emit(OpCodes.Ldloca_S, 0);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4, arrayIndex++);
                generator.Emit(OpCodes.Ldelem_Ref);
                if (ParseMethodMap.ContainsKey(field.FieldType))
                    generator.Emit(OpCodes.Call, ParseMethodMap[field.FieldType]);
                generator.Emit(OpCodes.Stfld, field);
            }
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Box, valueType);
            generator.Emit(OpCodes.Ret);

            var parser = (CsvParser)method.CreateDelegate(typeof (CsvParser));
            _csvParsers.Add(methodName, parser);
            return parser;
        }

        public CsvWriter GetOrGenerateCsvWriter(Type valueType)
        {
            var methodName = string.Format("Write{0}ToCsv", valueType.Name);
            if (_csvWriters.ContainsKey(methodName))
                return _csvWriters[methodName];

            var method = new DynamicMethod(methodName, typeof(string), new[] { typeof(IData) });
            var generator = method.GetILGenerator();
            var localData = generator.DeclareLocal(valueType);
            generator.DeclareLocal(typeof (StringBuilder)); // local StringBuilder
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Unbox_Any, valueType);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Newobj, typeof(StringBuilder).GetConstructor(new Type[0]));
            generator.Emit(OpCodes.Stloc_1);
            var fields = valueType.GetFields();
            for (var index = 0; index < fields.Length; ++index)
            {
                var field = fields[index];
                generator.Emit(OpCodes.Ldloc_1);
                generator.Emit(OpCodes.Ldloca_S, localData);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new [] { field.FieldType }));

                if (index != fields.Length - 1)
                {
                    generator.Emit(OpCodes.Ldstr, ",");
                    generator.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }));
                }
                generator.Emit(OpCodes.Pop);
            }
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
            generator.Emit(OpCodes.Ret);

            var writer = (CsvWriter)method.CreateDelegate(typeof(CsvWriter));
            _csvWriters.Add(methodName, writer);
            return writer;
        }

        public BinParser GetOrGenerateBinParser(Type valueType)
        {
            var methodName = string.Format("Parse{0}FromBin", valueType.Name);
            if (_binParsers.ContainsKey(methodName))
                return _binParsers[methodName];

            var method = new DynamicMethod(methodName, typeof(IData), new[] { typeof(byte[]), typeof(int).MakeByRefType() });
            var generator = method.GetILGenerator();
            var localData = generator.DeclareLocal(valueType);
            generator.DeclareLocal(typeof(Int32)); // variable of string's length
            generator.Emit(OpCodes.Ldloca_S, localData);
            generator.Emit(OpCodes.Initobj, valueType);
            foreach (var field in valueType.GetFields())
            {
                if (field.FieldType != typeof (string))
                {
                    // read and set value
                    generator.Emit(OpCodes.Ldloca_S, localData);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldind_I4);
                    if (field.FieldType == typeof(byte))
                    {
                        generator.Emit(OpCodes.Ldelem_U1);
                    }
                    else if (field.FieldType == typeof(DateTime))
                    {
                        generator.Emit(OpCodes.Call, BitConverterMap[field.FieldType]);
                        generator.Emit(OpCodes.Newobj, typeof(DateTime).GetConstructor(new[] { typeof(Int64) }));
                    }
                    else
                    {
                        generator.Emit(OpCodes.Call, BitConverterMap[field.FieldType]);
                    }
                    generator.Emit(OpCodes.Stfld, field);

                    // increase offset
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldind_I4);
                    generator.Emit(OpCodes.Ldc_I4, TypeSizeMap[field.FieldType]);
                    generator.Emit(OpCodes.Add);
                    generator.Emit(OpCodes.Stind_I4);
                }
                else
                {
                    // read length
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldind_I4);
                    generator.Emit(OpCodes.Call, BitConverterMap[typeof(Int32)]);
                    generator.Emit(OpCodes.Stloc_1);

                    // increase length's offset
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldind_I4);
                    generator.Emit(OpCodes.Ldc_I4, TypeSizeMap[typeof(Int32)]);
                    generator.Emit(OpCodes.Add);
                    generator.Emit(OpCodes.Stind_I4);

                    // read and set string
                    generator.Emit(OpCodes.Ldloca_S, localData);
                    generator.Emit(OpCodes.Call, typeof(Encoding).GetMethod("get_UTF8", new Type[0]));
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldind_I4);
                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Callvirt, typeof(Encoding).GetMethod("GetString", new[] {typeof(byte[]), typeof(Int32), typeof(Int32)}));
                    generator.Emit(OpCodes.Stfld, field);

                    // increase offset
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldind_I4);
                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Add);
                    generator.Emit(OpCodes.Stind_I4);
                }
            }
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Box, valueType);
            generator.Emit(OpCodes.Ret);

            var parser = (BinParser)method.CreateDelegate(typeof(BinParser));
            _binParsers.Add(methodName, parser);
            return parser;
        }

        public BinWriter GetOrGenerateBinWriter(Type valueType)
        {
            var methodName = string.Format("Write{0}ToBin", valueType.Name);
            if (_binWriters.ContainsKey(methodName))
                return _binWriters[methodName];

            var method = new DynamicMethod(methodName, typeof(void), new[] { typeof(BinaryWriter), typeof(IData) });
            var generator = method.GetILGenerator();
            var localData = generator.DeclareLocal(valueType);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Unbox_Any, valueType);
            generator.Emit(OpCodes.Stloc_0);
            foreach (var field in valueType.GetFields())
            {
                if (field.FieldType != typeof (string))
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldloca_S, localData);
                    if (field.FieldType == typeof (DateTime))
                    {
                        generator.Emit(OpCodes.Ldflda, field);
                        generator.Emit(OpCodes.Call, typeof(DateTime).GetMethod("get_Ticks", new Type[0]));
                        generator.Emit(OpCodes.Callvirt, typeof(BinaryWriter).GetMethod("Write", new[] { typeof(Int64) }));
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldfld, field);
                        generator.Emit(OpCodes.Callvirt, typeof(BinaryWriter).GetMethod("Write", new[] { field.FieldType }));
                    }
                }
                else
                {
                    // write string's length
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Call, typeof(Encoding).GetMethod("get_UTF8", new Type[0]));
                    generator.Emit(OpCodes.Ldloca_S, localData);
                    generator.Emit(OpCodes.Ldfld, field);
                    generator.Emit(OpCodes.Callvirt, typeof (Encoding).GetMethod("GetByteCount", new[] {typeof (string)}));
                    generator.Emit(OpCodes.Callvirt, typeof (BinaryWriter).GetMethod("Write", new[] {typeof (Int32)}));

                    // write string's bytes
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Call, typeof(Encoding).GetMethod("get_UTF8", new Type[0]));
                    generator.Emit(OpCodes.Ldloca_S, localData);
                    generator.Emit(OpCodes.Ldfld, field);
                    generator.Emit(OpCodes.Callvirt, typeof(Encoding).GetMethod("GetBytes", new[] { typeof(string) }));
                    generator.Emit(OpCodes.Callvirt, typeof(BinaryWriter).GetMethod("Write", new[] { typeof(byte[]) }));

                }
            }
            generator.Emit(OpCodes.Ret);

            var writer = (BinWriter)method.CreateDelegate(typeof(BinWriter));
            _binWriters.Add(methodName, writer);
            return writer;
        }

        private static readonly Dictionary<Type, MethodInfo> ParseMethodMap = new Dictionary<Type, MethodInfo>
        {
            {typeof(Byte), typeof(Byte).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(Int16), typeof(Int16).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(Int32), typeof(Int32).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(Int64), typeof(Int64).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(UInt16), typeof(UInt16).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(UInt32), typeof(UInt32).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(UInt64), typeof(UInt64).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(Single), typeof(Single).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(Double), typeof(Double).GetMethod("Parse", new [] { typeof(string) })},
            {typeof(DateTime), typeof(DateTime).GetMethod("Parse", new [] { typeof(string) })},
        };

        private static readonly Dictionary<Type, MethodInfo> BitConverterMap = new Dictionary<Type, MethodInfo>
        {
            {typeof(Int16), typeof(BitConverter).GetMethod("ToInt16", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(Int32), typeof(BitConverter).GetMethod("ToInt32", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(Int64), typeof(BitConverter).GetMethod("ToInt64", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(UInt16), typeof(BitConverter).GetMethod("ToUInt16", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(UInt32), typeof(BitConverter).GetMethod("ToUInt32", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(UInt64), typeof(BitConverter).GetMethod("ToUInt64", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(Single), typeof(BitConverter).GetMethod("ToSingle", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(Double), typeof(BitConverter).GetMethod("ToDouble", new [] { typeof(byte[]), typeof(Int32) })},
            {typeof(DateTime), typeof(BitConverter).GetMethod("ToInt64", new [] { typeof(byte[]), typeof(Int32) })},
        };

        private static readonly Dictionary<Type, int> TypeSizeMap = new Dictionary<Type, int>
        {
            {typeof(Byte), sizeof(Byte)},
            {typeof(Int16), sizeof(Int16)},
            {typeof(Int32), sizeof(Int32)},
            {typeof(Int64), sizeof(Int64)},
            {typeof(UInt16), sizeof(UInt16)},
            {typeof(UInt32), sizeof(UInt32)},
            {typeof(UInt64), sizeof(UInt64)},
            {typeof(Single), sizeof(Single)},
            {typeof(Double), sizeof(Double)},
            {typeof(DateTime), sizeof(Int64)},
        };
    }
}
