using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Utilities;
using Shouldly;

namespace EngineUnitTests.Utilities;

public class StringFunctionsTests
{
    [Fact]
    public void IncrementNumberAtEnd_ShouldIncrementNumber()
    {
        StringFunctions.IncrementNumberAtEnd("Test").ShouldBe("Test1");
        StringFunctions.IncrementNumberAtEnd("Test1").ShouldBe("Test2");
        StringFunctions.IncrementNumberAtEnd("Test9").ShouldBe("Test10");
        StringFunctions.IncrementNumberAtEnd("Test01").ShouldBe("Test02");
        StringFunctions.IncrementNumberAtEnd("Test09").ShouldBe("Test10");
        StringFunctions.IncrementNumberAtEnd("Test99").ShouldBe("Test100");
        StringFunctions.IncrementNumberAtEnd("Test009").ShouldBe("Test010");
    }

}
