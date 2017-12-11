using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClrRuntime;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            unsafe
            {
                int[] a = new int[100];
                int[] b = Enumerable.Range(0, 100).ToArray();

                GCHandle.Alloc(a, GCHandleType.Pinned);
                var srcA = Marshal.UnsafeAddrOfPinnedArrayElement(a, 0);

                GCHandle.Alloc(b, GCHandleType.Pinned);
                var srcB = Marshal.UnsafeAddrOfPinnedArrayElement(b, 0);

                Runtime.Copy(srcB, a, 0, a.Length);

                KeyValuePair<int,int> k1 = new KeyValuePair<int,int>(), k2 = new KeyValuePair<int,int>(5,10);
                var pter  = Runtime.GetPtr(ref k1);
                Runtime.SetValue(ref k2, pter);
                var size = Runtime.SizeOf<KeyValuePair<int, int>>();

                Array.Clear(a,0,a.Length);
                Runtime.Copy(srcB, srcA, sizeof(int) * a.Length);

                int* p0 = (int*)Runtime.GetPointer(a, 5);
                *p0 = 500;

                p0 = (int*)Runtime.GetPtr(a, 5).ToPointer();
                *p0 = 600;

                static_cast();
            }
        }

        public static void static_cast()
        {
            BaseClass baseClass = new DerClass();


            int timea = Environment.TickCount;

            for (int i = 0; i < 100000; i++)
            {
                DerClass derClass1 = Runtime.StaticCast<DerClass>(baseClass);   
            }            

            int time_static = Environment.TickCount - timea;

            int timeb = Environment.TickCount;
            for (int i = 0; i < 100000; i++)
            {
                DerClass derClass = (DerClass)baseClass;
            }
            int time_cast = Environment.TickCount - timeb;

            bool fast = time_static < time_cast;
        }
    }

    public class BaseClass
    {
        public virtual string Name { get { return "ansel"; } }
    }

    public class DerClass : BaseClass
    {
        public int Id { get { return 0; } }
        public override string Name
        {
            get
            {
                return "castro";
            }
        }
    }
}
