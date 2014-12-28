using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using Actors;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FakeItEasy;
using System.IO;
using ActorGui;

namespace ActorGuiTest
{
    [TestFixture]
    public class PrimesTest
    {
        [Test]
        public void NormalOperation()
        {
            CheckPrimes(1, 1);
            CheckPrimes(2, 1, 2);
            CheckPrimes(3, 1, 2, 3);
            CheckPrimes(4, 1, 2, 3);
            CheckPrimes(5, 1, 2, 3, 5);
            CheckPrimes(6, 1, 2, 3, 5);
            CheckPrimes(7, 1, 2, 3, 5, 7);
            CheckPrimes(20, 1, 2, 3, 5, 7, 11, 13, 17, 19);
            CheckPrimes(30, 1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29);
            CheckPrimes(50, 1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47);
        }

        private static void CheckPrimes(int n, params int[] expected)
        {
            bool done = false;
            string result = "";
            IRuntime runtime = A.Fake<IRuntime>();

            Action<int, int, object, int> send = (actorId, code, payload, sender) =>
            {
                result = (string)payload;
            };
            A.CallTo(() => runtime.SendMessage(40, CallResult.Completed, A<string>.Ignored, 0))
                .Invokes(send);
            A.CallTo(() => runtime.RemoveActor(30)).Invokes(() => { done = true; });

            var prime = new GuiMachines.PrimeCalculator();
            prime.N = n;
            prime.Client = 40;
            while (!done)
            {
                prime.OnMessage(runtime, 30, new Message(Codes.Pulse, 0, null, 0));
            }
            int[] actual = ParseResult(result);
            CollectionAssert.AreEqual(expected, actual);
        }

        private static int[] ParseResult(string result)
        {
            string[] parts = result.Split(new char[] { '\n' }, StringSplitOptions.None);
            int[] primes = parts
                    .Skip(1)
                    .Select(Int32.Parse)
                    .ToArray();
            return primes;
        }
    }


}
