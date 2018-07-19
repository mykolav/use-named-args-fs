module UseNamedArgs.Assert

open System
open Expecto
open UseNamedArgs.TestsSupport.Contract

type Assert() = 
    interface IAssert with
        member this.True(actual: bool, message: string) =
            Expect.isTrue actual message
        member this.Equal(expected: 't, actual: 't) =
            Expect.equal (actual :> IEquatable<'t>) (expected :> IEquatable<'t>) ""
