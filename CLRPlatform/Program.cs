using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;


namespace CLRPlatform
{
    class Program
    {
        static AssemblyName aName;
        static ModuleBuilder mb;
        static AssemblyBuilder assembly;
        static TypeBuilder typeBuilder;
        static Type type;

        static void Main(string[] args)
        {
            var name = typeof(Program).Assembly.GetName();
            aName = new AssemblyName("ClrRuntime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            AppDomain myDomain = Thread.GetDomain();// AppDomain.CreateDomain("CLRPlatform");
            assembly = myDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            mb = assembly.DefineDynamicModule("ClrRuntime", "ClrRuntime.dll");

            typeBuilder = mb.DefineType("ClrRuntime.Runtime", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, typeof(System.Object));

            unsafe
            {
                DefineCopyMemoryPtrArray();
                DefineCopyMemoryArrayPtr();

                DefineGetIntPtr(typeof(IntPtr));
                DefineGetIntPtr(typeof(void*));

                DefineGetValue(typeof(IntPtr));
                DefineGetValue(typeof(void*));

                DefineSetValue(typeof(IntPtr));
                DefineSetValue(typeof(void*));

                DefineSetValueRef();
                DefineSetValue();

                DefineSetValueCopy(typeof(IntPtr));
                DefineSetValueCopy(typeof(void*));

                DefineCopy(typeof(IntPtr));
                DefineCopy(typeof(void*));

                DefineSizeOf();
                DefineGetElemAddr();
                DefineGetElemAPter();

                DefineCopyArray();
                DefineCopyArrayIndex();

                DefineStaticCast();
            }

            type = typeBuilder.CreateType();
            assembly.Save("ClrRuntime.dll");

            //TestDefineMemoryPtrArray();
            //TestDefineMemoryArrayPtr();

        }

        //public static void DefineCopyMemory()
        //{            
        //    var method = typeBuilder.DefineMethod("CopyMemory", MethodAttributes.Static | MethodAttributes.Public);
        //    GenericTypeParameterBuilder[] genTypeParams = method.DefineGenericParameters("T1", "T2");
        //    method.SetReturnType(typeof(void));

        //    var param = new Type[]
        //    {
        //         genTypeParams[0].MakeArrayType(),
        //         genTypeParams[1].MakeArrayType(),
        //         typeof(int)
        //    };

        //    method.SetParameters(param);
        //    method.DefineParameter(0, ParameterAttributes.None, "src");
        //    method.DefineParameter(1, ParameterAttributes.None, "dest");
        //    method.DefineParameter(2, ParameterAttributes.None, "src");
            
        //}

        /// <summary>
        /// CopyMemory(IntPtr src, Array desc, int index, int count)
        /// </summary>
        public static void DefineCopyMemoryPtrArray()
        {
            var method = typeBuilder.DefineMethod("Copy", MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            method.SetReturnType(typeof(void));

            var paramTypes = new Type[]
            {                 
                 typeof(IntPtr), //src
                 genParams[0].MakeArrayType(), //array
                 typeof(int), // index
                 typeof(int) //count
            };

            method.SetParameters(paramTypes);
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.In, "dest");
            method.DefineParameter(3, ParameterAttributes.In, "index");
            method.DefineParameter(4, ParameterAttributes.In, "count");

            var il = method.GetILGenerator();
            var ok= il.DefineLabel();           

            //if(src == IntPtr.Zero) throw ArgumentException(src);
            il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldsfld, typeof(IntPtr).GetField("Zero"));
            //il.EmitCall(OpCodes.Call, typeof(IntPtr).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public) , null);
            //il.Emit(OpCodes.Ldc_I4_0);
            //il.Emit(OpCodes.Ceq);            
            il.Emit(OpCodes.Brtrue_S, ok);
            il.Emit(OpCodes.Ldstr, "src");
            il.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }));
            il.Emit(OpCodes.Throw);

            //if (dest == null) throw new ArgumentNullException("dest");
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Brtrue_S, ok);
            il.Emit(OpCodes.Ldstr, "dest");
            il.Emit(OpCodes.Newobj, typeof(ArgumentNullException).GetConstructor(new Type[] { typeof(string) }));
            il.Emit(OpCodes.Throw);

            il.MarkLabel(ok);
            il.Emit(OpCodes.Ldarg_1); //load dest
            il.Emit(OpCodes.Ldarg_2); //load index
            il.Emit(OpCodes.Ldelema, genParams[0]); //load addr of dest[index] 
            il.Emit(OpCodes.Ldarg_0); //load src 
            //il.Emit(OpCodes.Call, typeof(IntPtr).GetMethod("ToPointer"));

            il.Emit(OpCodes.Sizeof, genParams[0]); //load size of T
            il.Emit(OpCodes.Ldarg_3); //load count
            il.Emit(OpCodes.Mul);     //load size
            il.Emit(OpCodes.Cpblk);

            il.Emit(OpCodes.Ret);
            
        }

        public static void DefineCopyMemoryArrayPtr()
        {
            var method = typeBuilder.DefineMethod("Copy", MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            method.SetReturnType(typeof(void));

            var paramTypes = new Type[]
            {                 
                 genParams[0].MakeArrayType(), //src array
                 typeof(IntPtr), //dest
                 typeof(int), // index
                 typeof(int) //count
            };

            method.SetParameters(paramTypes);
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "dest");
            method.DefineParameter(3, ParameterAttributes.None, "index");
            method.DefineParameter(4, ParameterAttributes.None, "count");

            var il = method.GetILGenerator();
            var ok = il.DefineLabel();

            //if(src == IntPtr.Zero) throw ArgumentException(src);
            il.Emit(OpCodes.Ldarg_0);                  
            il.Emit(OpCodes.Brtrue_S, ok);
            il.Emit(OpCodes.Ldstr, "src");
            il.Emit(OpCodes.Newobj, typeof(ArgumentNullException).GetConstructor(new Type[] { typeof(string) }));
            il.Emit(OpCodes.Throw);

            //if (dest == null) throw new ArgumentNullException("dest");
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Brtrue_S, ok);
            il.Emit(OpCodes.Ldstr, "dest");
            il.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }));
            il.Emit(OpCodes.Throw);

            il.MarkLabel(ok);
            il.Emit(OpCodes.Ldarg_1); //load dest   
            il.Emit(OpCodes.Ldarg_0); //load src array
            il.Emit(OpCodes.Ldarg_2); //load index
            il.Emit(OpCodes.Ldelema, genParams[0]); //load addr of src[index]                       

            il.Emit(OpCodes.Sizeof, genParams[0]); //load size of T
            il.Emit(OpCodes.Ldarg_3); //load count
            il.Emit(OpCodes.Mul);     //load size
            il.Emit(OpCodes.Cpblk);

            il.Emit(OpCodes.Ret);

        }

        public static void DefineGetIntPtr(Type input)
        {
            string name = input == typeof(IntPtr) ? "GetPtr" : "GetPointer";
            var method = typeBuilder.DefineMethod(name, MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetReturnType(input);            
            method.SetParameters(genParams[0].MakeByRefType());
            method.DefineParameter(1, ParameterAttributes.None, "value");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Conv_I);
            if(input == typeof(IntPtr))
                il.Emit(OpCodes.Newobj, typeof(IntPtr).GetConstructor(new Type[]{typeof(void*)}));

            il.Emit(OpCodes.Ret);
        }

        public static void DefineGetValue(Type input)
        {
            var method = typeBuilder.DefineMethod("GetValue", MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetReturnType(genParams[0]);
            method.SetParameters(input);
            method.DefineParameter(1, ParameterAttributes.None, "value");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldobj, genParams[0]);            
            il.Emit(OpCodes.Ret);
        }

        public static void DefineSetValue(Type input)
        {
            //SetValue(ref T src, IntPtr dest )
            var method = typeBuilder.DefineMethod("SetValue", MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetReturnType(typeof(void));
            method.SetParameters(new Type[] { genParams[0].MakeByRefType(), input });
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "dest");

            var il = method.GetILGenerator();
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldobj);
            //il.Emit(OpCodes.Stobj, genParams[0]);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Cpobj, genParams[0]);

            il.Emit(OpCodes.Ret);
        }

        public static void DefineSetValueRef()
        {
            //SetValue<T,V>(ref T src, ref V dest )
            var method = typeBuilder.DefineMethod("SetValue", MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T", "V");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            genParams[1].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetReturnType(typeof(void));
            method.SetParameters(new Type[] { genParams[0].MakeByRefType(), genParams[1].MakeByRefType() });
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "dest");

            var il = method.GetILGenerator();
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldobj);
            //il.Emit(OpCodes.Stobj, genParams[0]);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Cpobj, genParams[0]);

            il.Emit(OpCodes.Ret);
        }

        public static void DefineSetValue()
        {
            //SetValue<T,V>(T src, ref V dest )
            var method = typeBuilder.DefineMethod("SetValue", MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T", "V");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            genParams[1].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetReturnType(typeof(void));
            method.SetParameters(new Type[] { genParams[0], genParams[1].MakeByRefType() });
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "dest");

            var il = method.GetILGenerator();
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldobj);
            //il.Emit(OpCodes.Stobj, genParams[0]);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stobj, genParams[0]);

            il.Emit(OpCodes.Ret);
        }

        public static void DefineSetValueCopy(Type input)
        {
            //SetValue(ref T src, IntPtr dest )
            var method = typeBuilder.DefineMethod("SetValue", MethodAttributes.Static | MethodAttributes.Public);
            var genParams = method.DefineGenericParameters("T");
            genParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetReturnType(typeof(void));
            method.SetParameters(new Type[] { genParams[0], input });
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "dest");

            var il = method.GetILGenerator();
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldobj);
            //il.Emit(OpCodes.Stobj, genParams[0]);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stobj, genParams[0]);

            il.Emit(OpCodes.Ret);
        }

        public static void DefineCopy(Type type)
        {
            var method = typeBuilder.DefineMethod("Copy", MethodAttributes.Static | MethodAttributes.Public);
            method.SetReturnType(typeof(void));
            method.SetParameters(type, type, typeof(int));
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "dest");
            method.DefineParameter(3, ParameterAttributes.None, "bytes");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Cpblk);

            il.Emit(OpCodes.Ret);
        }

        public static void DefineCopyArray()
        {
            var method = typeBuilder.DefineMethod("Copy", MethodAttributes.Static | MethodAttributes.Public);
            method.SetReturnType(typeof(void));
            var genParam = method.DefineGenericParameters("T1", "T2");
            method.SetParameters(genParam[0].MakeArrayType(), genParam[1].MakeArrayType(), typeof(int));
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "dest");
            method.DefineParameter(3, ParameterAttributes.None, "count");

            var il = method.GetILGenerator();
            //dest
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldelema, genParam[1]);

            //src
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldelema, genParam[0]);

            //size
            il.Emit(OpCodes.Sizeof, genParam[0]);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Cpblk);

            il.Emit(OpCodes.Ret);
        }

        public static void DefineCopyArrayIndex()
        {
            var method = typeBuilder.DefineMethod("Copy", MethodAttributes.Static | MethodAttributes.Public);
            method.SetReturnType(typeof(void));
            var genParam = method.DefineGenericParameters("T1", "T2");
            method.SetParameters(genParam[0].MakeArrayType(),typeof(int), genParam[1].MakeArrayType(), typeof(int), typeof(int));
            method.DefineParameter(1, ParameterAttributes.None, "src");
            method.DefineParameter(2, ParameterAttributes.None, "srcIndex");
            method.DefineParameter(3, ParameterAttributes.None, "dest");
            method.DefineParameter(4, ParameterAttributes.None, "destIndex");
            method.DefineParameter(5, ParameterAttributes.None, "count");

            var il = method.GetILGenerator();
            //dest
            il.Emit(OpCodes.Ldarg_S, 2);
            il.Emit(OpCodes.Ldarg_S, 3);
            il.Emit(OpCodes.Ldelema, genParam[1]);

            //src
            il.Emit(OpCodes.Ldarg_S, 0);
            il.Emit(OpCodes.Ldarg_S, 1);
            il.Emit(OpCodes.Ldelema, genParam[0]);

            //size
            il.Emit(OpCodes.Sizeof, genParam[0]);
            il.Emit(OpCodes.Ldarg_S, 4);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Cpblk);

            il.Emit(OpCodes.Ret);
        }

        public static void DefineSizeOf()
        {
            var method = typeBuilder.DefineMethod("SizeOf", MethodAttributes.Static | MethodAttributes.Public);
            method.SetReturnType(typeof(int));
            var genParamType = method.DefineGenericParameters("T");
            genParamType[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Sizeof, genParamType[0]);
            il.Emit(OpCodes.Ret);

        }

        public static void DefineGetElemAddr()
        {
            var method = typeBuilder.DefineMethod("GetPtr", MethodAttributes.Static | MethodAttributes.Public);
            method.SetReturnType(typeof(IntPtr));
            var genParam = method.DefineGenericParameters("T");
            genParam[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetParameters(genParam[0].MakeArrayType(), typeof(int));
            method.DefineParameter(1, ParameterAttributes.None, "array");
            method.DefineParameter(2, ParameterAttributes.HasDefault, "index").SetConstant(0);

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); //load array
            il.Emit(OpCodes.Ldarg_1); //load index
            il.Emit(OpCodes.Ldelema, genParam[0]); //load addr of array[index] 
            il.Emit(OpCodes.Ret);
        }

        public static void DefineGetElemAPter()
        {
            var method = typeBuilder.DefineMethod("GetPointer", MethodAttributes.Static | MethodAttributes.Public);
            method.SetReturnType(typeof(void*));
            var genParam = method.DefineGenericParameters("T");
            genParam[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);

            method.SetParameters(genParam[0].MakeArrayType(), typeof(int));
            method.DefineParameter(1, ParameterAttributes.None, "array");
            method.DefineParameter(2, ParameterAttributes.HasDefault, "index").SetConstant(0);

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); //load array
            il.Emit(OpCodes.Ldarg_1); //load index
            il.Emit(OpCodes.Ldelema, genParam[0]); //load addr of array[index] 
            il.Emit(OpCodes.Ret);
        }

        public static void DefineStaticCast()
        {
            var method = typeBuilder.DefineMethod("StaticCast", MethodAttributes.Static | MethodAttributes.Public);
            var genParamType = method.DefineGenericParameters("T");            
            genParamType[0].SetGenericParameterAttributes(GenericParameterAttributes.ReferenceTypeConstraint);
            method.SetReturnType(genParamType[0]);

            method.SetParameters(typeof(object));
            method.DefineParameter(1, ParameterAttributes.None, "value");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
        }

        #region Tests

        public static void TestDefineMemoryPtrArray()
        {
            var method = type.GetMethod("CopyPtrToArray", BindingFlags.Public | BindingFlags.Static);
            unsafe
            {
                int[] a = new int[100];
                int[] b = Enumerable.Range(0, 100).ToArray();
                GCHandle.Alloc(b, GCHandleType.Pinned);

                var gen = method.MakeGenericMethod(typeof(int));
                gen.Invoke(null, new object[] { Marshal.UnsafeAddrOfPinnedArrayElement(b, 0), a, 0, a.Length });

                for (int i = 0; i < a.Length; i++)
                {
                    if(a[i]!=b[i])
                        throw new Exception("TestDefineMemoryPtrArray Fail");
                }
            }

        }

        public static void TestDefineMemoryArrayPtr()
        {
            var method = type.GetMethod("CopyArrayToPtr", BindingFlags.Public | BindingFlags.Static);
            unsafe
            {
                int[] a = new int[100];
                int[] b = Enumerable.Range(0, 100).ToArray();
                GCHandle.Alloc(a, GCHandleType.Pinned);

                var gen = method.MakeGenericMethod(typeof(int));
                gen.Invoke(null, new object[] { b, Marshal.UnsafeAddrOfPinnedArrayElement(a, 0), 0, a.Length });

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                        throw new Exception("TestDefineMemoryPtrArray Fail");
                }
            }

        }

        static void TestGetIntPtr()
        {
            
        }
        #endregion
      
    }
    
}
