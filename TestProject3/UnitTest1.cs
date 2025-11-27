using System.Security.Cryptography.X509Certificates;
using System;
using Xunit;

namespace TestProject3
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            string a = Calcu.Calculator.Calculator1(2, 3, 3);
            Assert.Equal("5", a);
            
        }
    }


    public class UnitTest2
    {
        [Fact]
        public void Test2()
        {

        }
    }
}