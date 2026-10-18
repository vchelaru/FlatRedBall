using System.Collections;
using System.Reflection;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Content.Math;

/// <summary>
/// ShapeCollectionSave.Clone replaces the XmlSerializer round trip (FileManager.CloneObject) that AnimationFrame.Clone
/// used, which throws under Native AOT. The reflection tests populate and compare every public member of every Save
/// type reachable from ShapeCollectionSave, so a field added later that Clone forgets fails here.
/// </summary>
public class ShapeCollectionSaveCloneTests
{
    #region Reflection helpers

    static IEnumerable<MemberInfo> SettableMembers(Type type)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!field.IsInitOnly)
            {
                yield return field;
            }
        }
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
            {
                yield return property;
            }
        }
    }

    static Type TypeOf(MemberInfo member) =>
        member is FieldInfo field ? field.FieldType : ((PropertyInfo)member).PropertyType;

    static object GetValue(MemberInfo member, object instance) =>
        member is FieldInfo field ? field.GetValue(instance) : ((PropertyInfo)member).GetValue(instance);

    static void SetValue(MemberInfo member, object instance, object value)
    {
        if (member is FieldInfo field) field.SetValue(instance, value);
        else ((PropertyInfo)member).SetValue(instance, value);
    }

    /// <summary>Builds a value of the given type with every nested member set to something non-default and distinct.</summary>
    static object Populate(Type type, string path, ref int seed)
    {
        seed++;
        if (type == typeof(string)) return path + seed;
        if (type == typeof(bool)) return true;
        if (type == typeof(float)) return seed + 0.25f;
        if (type == typeof(double)) return seed + 0.5;
        if (type == typeof(int)) return seed;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(Enum.GetValues(type).Length - 1);

        if (type.IsArray)
        {
            Type elementType = type.GetElementType();
            Array array = Array.CreateInstance(elementType, 2);
            for (int i = 0; i < array.Length; i++)
            {
                array.SetValue(Populate(elementType, path + "[]", ref seed), i);
            }
            return array;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            IList list = (IList)Activator.CreateInstance(type);
            for (int i = 0; i < 2; i++)
            {
                list.Add(Populate(type.GetGenericArguments()[0], path + "[]", ref seed));
            }
            return list;
        }

        object instance = Activator.CreateInstance(type);
        foreach (MemberInfo member in SettableMembers(type))
        {
            // Width/Height are derived from ScaleX/ScaleY, populating them would just overwrite the scale
            if (member.Name == "Width" || member.Name == "Height") continue;
            SetValue(member, instance, Populate(TypeOf(member), member.Name, ref seed));
        }
        return instance;
    }

    /// <summary>Asserts equal values all the way down AND that no reference-type object is shared between the two graphs.</summary>
    static void AssertDeepCopy(object original, object clone, string path)
    {
        if (original == null)
        {
            clone.ShouldBeNull(path);
            return;
        }
        clone.ShouldNotBeNull(path);
        Type type = original.GetType();
        clone.GetType().ShouldBe(type, path);

        if (type.IsPrimitive || type.IsEnum || type == typeof(string))
        {
            clone.ShouldBe(original, path);
            return;
        }

        ReferenceEquals(original, clone).ShouldBeFalse(path + " is the same instance, not a copy");

        if (original is IList originalList)
        {
            IList cloneList = (IList)clone;
            cloneList.Count.ShouldBe(originalList.Count, path + ".Count");
            for (int i = 0; i < originalList.Count; i++)
            {
                AssertDeepCopy(originalList[i], cloneList[i], path + "[" + i + "]");
            }
            return;
        }

        foreach (MemberInfo member in SettableMembers(type))
        {
            AssertDeepCopyOfMember(member, original, clone, path);
        }
    }

    static void AssertDeepCopyOfMember(MemberInfo member, object original, object clone, string path)
    {
        object originalValue = GetValue(member, original);
        object cloneValue = GetValue(member, clone);
        string memberPath = path + "." + member.Name;

        Type memberType = TypeOf(member);
        if (memberType.IsValueType && !memberType.IsPrimitive && !memberType.IsEnum)
        {
            // struct such as Point: compare its members, a struct is always a copy
            foreach (MemberInfo inner in SettableMembers(memberType))
            {
                AssertDeepCopyOfMember(inner, originalValue, cloneValue, memberPath);
            }
        }
        else
        {
            AssertDeepCopy(originalValue, cloneValue, memberPath);
        }
    }

    #endregion

    [Theory]
    [InlineData(typeof(AxisAlignedRectangleSave))]
    [InlineData(typeof(AxisAlignedCubeSave))]
    [InlineData(typeof(CircleSave))]
    [InlineData(typeof(SphereSave))]
    [InlineData(typeof(PolygonSave))]
    public void Clone_ShapeSave_CopiesEveryPublicMember(Type saveType)
    {
        int seed = 0;
        object original = Populate(saveType, saveType.Name, ref seed);

        object clone = saveType.GetMethod("Clone", Type.EmptyTypes).ShouldNotBeNull().Invoke(original, null);

        AssertDeepCopy(original, clone, saveType.Name);
    }

    [Fact]
    public void Clone_ShapeCollectionSave_CopiesEveryPublicMember()
    {
        int seed = 0;
        ShapeCollectionSave original = (ShapeCollectionSave)Populate(typeof(ShapeCollectionSave), "ShapeCollectionSave", ref seed);
        // FileName is the one member that is [XmlIgnore]d, so the old XmlSerializer round trip dropped it
        original.FileName = "shapes.shcx";

        ShapeCollectionSave clone = original.Clone();

        AssertDeepCopy(original, clone, "ShapeCollectionSave");
    }

    [Fact]
    public void Clone_ShapeCollectionSave_PopulatesEveryShapeList()
    {
        // Guards the reflection tests above: if Populate silently skipped a list, the copy checks would pass vacuously.
        int seed = 0;
        ShapeCollectionSave original = (ShapeCollectionSave)Populate(typeof(ShapeCollectionSave), "ShapeCollectionSave", ref seed);

        original.AxisAlignedRectangleSaves.Count.ShouldBe(2);
        original.AxisAlignedCubeSaves.Count.ShouldBe(2);
        original.PolygonSaves.Count.ShouldBe(2);
        original.PolygonSaves[0].Points.Length.ShouldBe(2);
        original.CircleSaves.Count.ShouldBe(2);
        original.SphereSaves.Count.ShouldBe(2);
    }

    [Fact]
    public void Clone_ShapeCollectionSave_EditingCloneDoesNotChangeOriginal()
    {
        var original = new ShapeCollectionSave();
        original.AxisAlignedRectangleSaves.Add(new AxisAlignedRectangleSave { Name = "Rect", X = 1, ScaleX = 2 });
        original.CircleSaves.Add(new CircleSave { Name = "Circle", Radius = 3 });
        original.PolygonSaves.Add(new PolygonSave
        {
            Name = "Poly",
            Points = new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1) }
        });

        ShapeCollectionSave clone = original.Clone();
        clone.AxisAlignedRectangleSaves[0].X = 100;
        clone.AxisAlignedRectangleSaves[0].Name = "Changed";
        clone.CircleSaves.Clear();
        clone.PolygonSaves[0].Points[1] = new Point(50, 50);

        original.AxisAlignedRectangleSaves[0].X.ShouldBe(1);
        original.AxisAlignedRectangleSaves[0].Name.ShouldBe("Rect");
        original.CircleSaves.Count.ShouldBe(1);
        original.PolygonSaves[0].Points[1].X.ShouldBe(1);
    }

    [Fact]
    public void Clone_ShapeCollectionSave_ClonesAreIndependentOfOriginalAdditions()
    {
        var original = new ShapeCollectionSave();
        ShapeCollectionSave clone = original.Clone();

        original.SphereSaves.Add(new SphereSave());

        clone.SphereSaves.Count.ShouldBe(0);
    }

    [Fact]
    public void Clone_PolygonSaveWithNullPoints_StaysNull()
    {
        var original = new PolygonSave { Name = "NoPoints" };

        PolygonSave clone = original.Clone();

        clone.Points.ShouldBeNull();
        clone.Name.ShouldBe("NoPoints");
    }

    [Fact]
    public void Clone_AnimationFrame_DeepCopiesShapeCollectionSave()
    {
        var frame = new AnimationFrame { ShapeCollectionSave = new ShapeCollectionSave() };
        frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(
            new AxisAlignedRectangleSave { Name = "Rect", ScaleX = 4, OffsetInterpolation = "Linear" });

        AnimationFrame clone = frame.Clone();
        clone.ShapeCollectionSave.AxisAlignedRectangleSaves[0].ScaleX = 9;

        clone.ShapeCollectionSave.ShouldNotBeSameAs(frame.ShapeCollectionSave);
        clone.ShapeCollectionSave.AxisAlignedRectangleSaves[0].OffsetInterpolation.ShouldBe("Linear");
        frame.ShapeCollectionSave.AxisAlignedRectangleSaves[0].ScaleX.ShouldBe(4);
    }
}
