using System;
using System.Reflection;
using FlatRedBall.Instructions.Reflection;
using Shouldly;

namespace EngineUnitTests.Instructions;

public class EntityBase
{
    public string Name { get; set; }
}

public interface ILateBoundEntity
{
    float Speed { get; }
    int Health { get; }
    float Ratio { get; }
    string Label { get; }
    int PrivateSetterValue { get; }
}

// Each test class binds a distinct closed generic so LateBinder's per-type caches don't leak between the
// emit and reflection runs.
public class EmitTag { }
public class ReflectionTag { }

public class LateBoundEntity<TTag> : EntityBase, ILateBoundEntity
{
    public float Speed { get; set; }
    public int Health;
    public float Ratio;
    public string Label;
    public int PrivateSetterValue { get; private set; }
    public int ReadOnlyValue => 7;
    public int ThrowsOnSet
    {
        get => 0;
        set => throw new ArgumentException("boom from setter");
    }
    public int ThrowsOnGet => throw new ArgumentException("boom from getter");

    float ILateBoundEntity.Speed => Speed;
    int ILateBoundEntity.Health => Health;
    float ILateBoundEntity.Ratio => Ratio;
    string ILateBoundEntity.Label => Label;
}

public struct LateBoundStruct
{
    public int Value { get; set; }
}

/// <summary>
/// LateBinder emits DynamicMethod delegates where dynamic code is available and falls back to plain reflection
/// otherwise (Native AOT). Both modes must behave the same, so every test runs against both.
/// </summary>
public abstract class LateBinderTestsBase<TEntity> : IDisposable
    where TEntity : EntityBase, ILateBoundEntity, new()
{
    static readonly FieldInfo ForceReflectionField =
        typeof(LateBinder).GetField("ForceReflection", BindingFlags.NonPublic | BindingFlags.Static);

    protected LateBinderTestsBase(bool forceReflection)
    {
        ForceReflectionField.SetValue(null, forceReflection);
    }

    public void Dispose() => ForceReflectionField.SetValue(null, false);

    LateBinder Binder => LateBinder.GetInstance(typeof(TEntity));

    [Fact]
    public void SetValue_ShouldSetPublicProperty()
    {
        var entity = new TEntity();
        Binder.SetValue(entity, "Speed", 5f).ShouldBeTrue();
        entity.Speed.ShouldBe(5f);
    }

    [Fact]
    public void SetValue_ShouldSetPublicField()
    {
        var entity = new TEntity();
        Binder.SetValue(entity, "Health", 7).ShouldBeTrue();
        Binder.SetValue(entity, "Label", "hello").ShouldBeTrue();
        entity.Health.ShouldBe(7);
        entity.Label.ShouldBe("hello");
    }

    [Fact]
    public void SetValue_ShouldSetInheritedProperty()
    {
        var entity = new TEntity();
        Binder.SetValue(entity, "Name", "bob");
        entity.Name.ShouldBe("bob");
    }

    [Fact]
    public void SetValue_ShouldSetPrivateSetter()
    {
        var entity = new TEntity();
        Binder.SetValue(entity, "PrivateSetterValue", 3);
        entity.PrivateSetterValue.ShouldBe(3);
    }

    [Fact]
    public void SetValue_ShouldThrowInvalidOperation_WhenPropertyValueIsWrongType()
    {
        var entity = new TEntity();
        Should.Throw<InvalidOperationException>(() => Binder.SetValue(entity, "Speed", "not a float"));
    }

    [Fact]
    public void SetValue_ShouldSurfaceSetterException_NotTargetInvocationException()
    {
        var entity = new TEntity();
        var exception = Should.Throw<Exception>(() => Binder.SetValue(entity, "ThrowsOnSet", 1));
        exception.ShouldBeOfType<ArgumentException>();
        exception.Message.ShouldBe("boom from setter");
    }

    [Fact]
    public void SetValue_ShouldThrow_WhenMemberDoesNotExist()
    {
        var entity = new TEntity();
        Should.Throw<MemberAccessException>(() => Binder.SetValue(entity, "DoesNotExist", 1));
    }

    [Fact]
    public void GetValue_ShouldReturnProperty()
    {
        var entity = new TEntity();
        Binder.SetValue(entity, "Speed", 2.5f);
        Binder.GetValue(entity, "Speed").ShouldBe(2.5f);
        Binder.GetValue(entity, "ReadOnlyValue").ShouldBe(7);
    }

    [Fact]
    public void GetValue_ShouldReturnFieldAndInheritedProperty()
    {
        var entity = new TEntity();
        Binder.SetValue(entity, "Health", 9);
        Binder.SetValue(entity, "Name", "bob");
        Binder.GetValue(entity, "Health").ShouldBe(9);
        Binder.GetValue(entity, "Name").ShouldBe("bob");
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenGetterThrows()
    {
        var entity = new TEntity();
        // Existing behavior: LateBinder swallows getter exceptions so a property grid doesn't blow up.
        Binder.GetValue(entity, "ThrowsOnGet").ShouldBeNull();
    }

    [Fact]
    public void TryGetValue_ShouldReturnFalse_WhenMemberDoesNotExist()
    {
        var entity = new TEntity();
        Binder.TryGetValue(entity, "DoesNotExist", out var result).ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryGetValue_ShouldFindPropertiesAndFields()
    {
        var entity = new TEntity();
        Binder.SetValue(entity, "Speed", 1f);
        Binder.SetValue(entity, "Health", 2);
        Binder.TryGetValue(entity, "Speed", out var speed).ShouldBeTrue();
        Binder.TryGetValue(entity, "Health", out var health).ShouldBeTrue();
        speed.ShouldBe(1f);
        health.ShouldBe(2);
    }

    [Fact]
    public void SetValueStatic_ShouldUseRuntimeTypeOfTarget()
    {
        var entity = new TEntity();
        LateBinder.SetValueStatic(entity, "Speed", 4f).ShouldBeTrue();
        LateBinder.GetValueStatic(entity, "Speed").ShouldBe(4f);
    }

    [Fact]
    public void SetProperty_ShouldSetPropertyThroughTypedInstance()
    {
        var entity = new TEntity();
        LateBinder<TEntity>.Instance.SetProperty<float>(entity, "Speed", 11f);
        entity.Speed.ShouldBe(11f);
    }

    [Fact]
    public void SetProperty_ShouldSetFieldThroughTypedInstance()
    {
        var entity = new TEntity();
        LateBinder<TEntity>.Instance.SetProperty<int>(entity, "Health", 12);
        entity.Health.ShouldBe(12);
    }

    [Fact]
    public void SetProperty_ShouldThrow_WhenMemberDoesNotExist()
    {
        var entity = new TEntity();
        Should.Throw<MemberAccessException>(() => LateBinder<TEntity>.Instance.SetProperty<int>(entity, "DoesNotExist", 1));
    }

    [Fact]
    public void GetProperty_ShouldReturnTypedProperty()
    {
        var entity = new TEntity();
        LateBinder<TEntity>.Instance.SetProperty<float>(entity, "Speed", 6f);
        LateBinder<TEntity>.Instance.GetProperty<float>(entity, "Speed").ShouldBe(6f);
    }

    [Fact]
    public void GetField_ShouldReturnTypedField()
    {
        var entity = new TEntity();
        LateBinder<TEntity>.Instance.SetProperty<int>(entity, "Health", 8);
        LateBinder<TEntity>.Instance.GetField<int>(entity, "Health").ShouldBe(8);
    }

    [Fact]
    public void IsReadOnly_ShouldReflectPropertySetters()
    {
        Binder.IsReadOnly("ReadOnlyValue").ShouldBeTrue();
        Binder.IsReadOnly("Speed").ShouldBeFalse();
        Binder.IsReadOnly("Health").ShouldBeFalse();
    }

    [Fact]
    public void GetValue_ShouldWorkOnValueTypeTargets()
    {
        var boxed = (object)new LateBoundStruct { Value = 5 };
        LateBinder.GetInstance(typeof(LateBoundStruct)).GetValue(boxed, "Value").ShouldBe(5);
    }
}

[Collection("LateBinder")]
public class LateBinderEmitTests : LateBinderTestsBase<LateBoundEntity<EmitTag>>
{
    public LateBinderEmitTests() : base(forceReflection: false) { }
}

[Collection("LateBinder")]
public class LateBinderReflectionTests : LateBinderTestsBase<LateBoundEntity<ReflectionTag>>
{
    public LateBinderReflectionTests() : base(forceReflection: true) { }
}
