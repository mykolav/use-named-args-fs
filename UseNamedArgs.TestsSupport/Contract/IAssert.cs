using System;

namespace UseNamedArgs.TestsSupport.Contract
{
    public interface IAssert
    {
        void True(bool actual, string message);
        void Equal<T>(T expected, T actual) where T: IEquatable<T>;
    }
}
