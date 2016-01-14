using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;

namespace CodeGenerator.Generators
{

    public static class ShapeCollectionCollisionCodeGenerator
	{
		#region Fields

		#region Const strings useed for generated code

        #region ShapeCollectionCollision Header 

        const string NamespaceAndClassHeader =
@"
using System;

namespace FlatRedBall.Math.Geometry
{	
	internal static class ShapeCollectionCollision
	{";

        #endregion

        #region CollideShapeAgainstThis method


        // {0} - the Shape Type, like "AxisAlignedRectangle"
        // {1} - The type of collision.  Can be "", "Move", or "Bounce"
		// {2} - The extra arguments for collision.  Cna be "" or  ", float shapeMass, float collectionMass"
        const string MethodHeader =
@"		internal static bool CollideShapeAgainstThis{1}(ShapeCollection thisShapeCollection, {0} shapeToCollideAgainstThis, bool considerAxisBasedPartitioning, Axis axisToUse{2})";


        const string OpenBracket =
@"		{";

        const string ClearCollisionLists =
@"            thisShapeCollection.ClearCollisionLists();";



        const string DeclareVariables =
@"            #region Declare variables used for this method
            bool returnValue = false;

            int startIndex;
            int endIndex;

            #endregion";


        const string BoundStartPosition =
@"            #region Get the boundStartPosition

            float boundStartPosition = 0;

            switch (axisToUse)
            {
                case Axis.X:
					boundStartPosition = shapeToCollideAgainstThis.X;
                    break;
                case Axis.Y:
					boundStartPosition = shapeToCollideAgainstThis.Y;
                    break;
                case Axis.Z:
                    throw new ArgumentException();
                // break;
            }
            #endregion";




        // {0} - the list type, like AxisAlignedRectangles
        // {1} - The type of collision.  Can be "", "Move", or "Bounce"
        // {2} - The extra arguments for collision.  Can be 
                                //"", 
                                //, "shapeMass, collectionMass", or 
                                //", shapeMass, collectionMass, elasticity"
        const string ShapeVsListTemplate =
@"		    #region vs. {0}

		    GetStartAndEnd(
			    considerAxisBasedPartitioning, 
			    axisToUse, 
			    out startIndex, 
			    out endIndex, 
			    boundStartPosition, 
			    shapeToCollideAgainstThis.BoundingRadius,
			    // SET THIS:
			    thisShapeCollection.mMax{0}Radius, 
			    thisShapeCollection.m{0}
			    // END OF SET
			    );

            for (int i = startIndex; i < endIndex; i++)
            {{
                if (shapeToCollideAgainstThis.CollideAgainst{1}(thisShapeCollection.m{0}[i]{2}))
                {{
                    thisShapeCollection.mLastCollision{0}.Add(thisShapeCollection.m{0}[i]);
                    returnValue = true;
                }}
            }}

            #endregion";
        
        const string ReturnStatement =
@"            return returnValue;";

        const string CloseBracket =
@"		}";

        #endregion


        #region GetStartAndEndMethod


        const string GetStartAndEndMethod =
@"
		private static void GetStartAndEnd<T>(bool considerAxisBasedPartitioning, Axis axisToUse,
			out int startIndex, out int endIndex, float boundStartPosition, float individualShapeRadius, float listMaxRadius, PositionedObjectList<T> list) where T : PositionedObject
		{
			if (considerAxisBasedPartitioning)
			{
				float combinedRadii = individualShapeRadius + listMaxRadius;

				startIndex = list.GetFirstAfterPosition(
					boundStartPosition - combinedRadii,
					axisToUse,
					0,
					list.Count - 1);

				endIndex = list.GetFirstAfterPosition(
					boundStartPosition + combinedRadii,
					axisToUse,
					0,
					list.Count - 1);
			}
			else
			{
				startIndex = 0;
				endIndex = list.Count;
			}
		}
";

        #endregion

        const string CloseNamespaceAndClassBracket =
@"	}
}";

		#endregion

		#region Const strings used for code to be copied into the ShapeCollection class

		const string MethodBody =
@"		
		public bool CollideAgainst({0} {1})
		{{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, {1}, false, Axis.X);

		}}

		public bool CollideAgainst({0} {1}, bool considerPartitioning, Axis axisToUse)
		{{
			return ShapeCollectionCollision.CollideShapeAgainstThis(this, {1}, considerPartitioning, axisToUse);

		}}
";

		const string MoveMethodBody =
@"		

		public bool CollideAgainstMove({0} {1}, float thisMass, float otherMass)
		{{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, {1}, false, Axis.X, otherMass, thisMass);

		}}

		public bool CollideAgainstMove({0} {1}, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
		{{
			return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, {1}, considerPartitioning, axisToUse, otherMass, thisMass);

		}}
";

		const string BounceMethodBody =
@"		
		public bool CollideAgainstBounce({0} {1}, float thisMass, float otherMass, float elasticity)
		{{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, {1}, false, Axis.X, otherMass, thisMass, elasticity);

		}}

		public bool CollideAgainstBounce({0} {1}, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
		{{
			return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, {1}, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

		}}
";

		#endregion

		static StringBuilder stringBuilder = new StringBuilder();
        static List<string> shapes2D;
		static List<string> shapes3D;

		#endregion

		#region Methods

		public static void CreateShapeCollectionCollisionFile()
        {
            CreateShapes();

			stringBuilder.AppendLine(NamespaceAndClassHeader);

			#region Write collision methods against individual shapes.

			string shapeMass = "";
			string methodHeaderMassArguments = "";
			string extraMethodName = "";
            // Write CollideAgainst against all of the shapes
			WriteCollisionGroup(shapeMass, methodHeaderMassArguments, extraMethodName);


			shapeMass = ", shapeMass, collectionMass";
			methodHeaderMassArguments = ", float shapeMass, float collectionMass";
			extraMethodName = "Move";
            // Write CollideAgainstMove against all of the shapes
			WriteCollisionGroup(shapeMass, methodHeaderMassArguments, extraMethodName);

			shapeMass = ", shapeMass, collectionMass, elasticity";
			methodHeaderMassArguments = ", float shapeMass, float collectionMass, float elasticity";
			extraMethodName = "Bounce";
            // Write CollideAgainstBounce against all shapes
			WriteCollisionGroup(shapeMass, methodHeaderMassArguments, extraMethodName);
			#endregion

            #region Write collision methods between ShapeCollection and ShapeCollection

            shapeMass = "";
            methodHeaderMassArguments = "";
            extraMethodName = "";
            WriteCollisionAgainstShapeCollection(shapeMass, methodHeaderMassArguments, extraMethodName);

            shapeMass = ", shapeMass, collectionMass";
            methodHeaderMassArguments = ", float shapeMass, float collectionMass";
            extraMethodName = "Move";
            WriteCollisionAgainstShapeCollection(shapeMass, methodHeaderMassArguments, extraMethodName);

            shapeMass = ", shapeMass, collectionMass, elasticity";
            methodHeaderMassArguments = ", float shapeMass, float collectionMass, float elasticity";
            extraMethodName = "Bounce";
            WriteCollisionAgainstShapeCollection(shapeMass, methodHeaderMassArguments, extraMethodName);


            #endregion

            stringBuilder.AppendLine(GetStartAndEndMethod);


			WriteCodeToBeCopied();


			stringBuilder.AppendLine(CloseNamespaceAndClassBracket);


			FileManager.SaveText(stringBuilder.ToString(), @"V:\FlatRedBall\Math\Geometry\ShapeCollectionCollision.cs");
        }

		private static void WriteCollisionGroup(string shapeMass, string methodHeaderMassArguments, string extraMethodName)
		{
			foreach (string shape2D in shapes2D)
			{
				// remove the S
				string shape = shape2D.Substring(0, shape2D.Length - 1);
				AddMethodToStringBuilder(shape, extraMethodName, shapeMass, methodHeaderMassArguments, shapes2D);
			}

			foreach (string shape3D in shapes3D)
			{
				// remove the S
				string shape = shape3D.Substring(0, shape3D.Length - 1);
				AddMethodToStringBuilder(shape, extraMethodName, shapeMass, methodHeaderMassArguments, shapes3D);
			}

			stringBuilder.AppendLine();
		}

        private static void WriteCollisionAgainstShapeCollection(string shapeMass, string methodHeaderMassArguments, string extraMethodName)
        {
            stringBuilder.AppendLine(string.Format(MethodHeader, "ShapeCollection", extraMethodName, methodHeaderMassArguments));
            stringBuilder.AppendLine(OpenBracket);

            stringBuilder.AppendLine(
@"			thisShapeCollection.mSuppressLastCollisionClear = true;
            bool returnValue = false;
");

            foreach (string shape in shapes2D)
            {
                AppendShapeLoop(shape.Substring(0, shape.Length - 1), extraMethodName, shapeMass);
            }
            foreach (string shape in shapes3D)
            {
                AppendShapeLoop(shape.Substring(0, shape.Length - 1), extraMethodName, shapeMass);
            }

            stringBuilder.Append(@"
			thisShapeCollection.mSuppressLastCollisionClear = false;
            return returnValue;
");

            stringBuilder.AppendLine(CloseBracket);
        }

        private static void AppendShapeLoop(string shapeType, string collisionType, string shapeMass)
        {
            const string shapeLoop =
                @"
            for (int i = 0; i < shapeToCollideAgainstThis.{0}s.Count; i++)
			{{
                {0} shape = shapeToCollideAgainstThis.{0}s[i];

                returnValue |= ShapeCollectionCollision.CollideShapeAgainstThis{1}(thisShapeCollection, shape, false, Axis.X{2});
            }}
";

            stringBuilder.Append(string.Format(shapeLoop, shapeType, collisionType, shapeMass));

        }

        private static void AddMethodToStringBuilder(string shapeType, string collisionType, string callingArguments, string methodHeaderMassArguments, List<string> shapes)
        {
			stringBuilder.AppendLine(string.Format(MethodHeader, shapeType, collisionType, methodHeaderMassArguments));

            stringBuilder.AppendLine(OpenBracket);

            stringBuilder.AppendLine(ClearCollisionLists);

            stringBuilder.AppendLine(DeclareVariables);

            stringBuilder.AppendLine(BoundStartPosition);

            stringBuilder.AppendLine();

			for (int i = 0; i < shapes.Count; i++)
            {
                stringBuilder.AppendLine(string.Format(ShapeVsListTemplate,
					shapes[i],
					collisionType,
					callingArguments
                    ));
                
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine(ReturnStatement);

            stringBuilder.AppendLine(CloseBracket);
        }

        private static void CreateShapes()
        {
            shapes2D = new List<string>();
			shapes2D.Add("AxisAlignedRectangles");
			shapes2D.Add("Circles");
			shapes2D.Add("Polygons");
			shapes2D.Add("Lines");

			shapes2D.Add("Capsule2Ds");


			shapes3D = new List<string>();
			shapes3D.Add("Spheres");
			shapes3D.Add("AxisAlignedCubes");
		
		}

		private static void WriteCodeToBeCopied()
		{
			stringBuilder.AppendLine("/*");
			stringBuilder.AppendLine("// Copy this code into ShapeCollection");
			stringBuilder.AppendLine("		#region Generated collision calling code");

			foreach (string tempShape in shapes2D)
			{
				char lowerCaseLetter = (char)((int)(tempShape[0]) + 32);
				string shape = lowerCaseLetter.ToString() + tempShape.Substring(1);

				stringBuilder.AppendLine(string.Format(MethodBody, tempShape.Substring(0, tempShape.Length - 1), shape.Substring(0, shape.Length - 1)));
			}
			foreach (string tempShape in shapes3D)
			{
				char lowerCaseLetter = (char)((int)(tempShape[0]) + 32);
				string shape = lowerCaseLetter.ToString() + tempShape.Substring(1);

				stringBuilder.AppendLine(string.Format(MethodBody, tempShape.Substring(0, tempShape.Length - 1), shape.Substring(0, shape.Length - 1)));
			}

			foreach (string tempShape in shapes2D)
			{
				char lowerCaseLetter = (char)((int)(tempShape[0]) + 32);
				string shape = lowerCaseLetter.ToString() + tempShape.Substring(1);

				stringBuilder.AppendLine(string.Format(MoveMethodBody, tempShape.Substring(0, tempShape.Length - 1), shape.Substring(0, shape.Length - 1)));
			}
			foreach (string tempShape in shapes3D)
			{
				char lowerCaseLetter = (char)((int)(tempShape[0]) + 32);
				string shape = lowerCaseLetter.ToString() + tempShape.Substring(1);

				stringBuilder.AppendLine(string.Format(MoveMethodBody, tempShape.Substring(0, tempShape.Length - 1), shape.Substring(0, shape.Length - 1)));
			}

			foreach (string tempShape in shapes2D)
			{
				char lowerCaseLetter = (char)((int)(tempShape[0]) + 32);
				string shape = lowerCaseLetter.ToString() + tempShape.Substring(1);

				stringBuilder.AppendLine(string.Format(BounceMethodBody, tempShape.Substring(0, tempShape.Length - 1), shape.Substring(0, shape.Length - 1)));
			}
			foreach (string tempShape in shapes3D)
			{
				char lowerCaseLetter = (char)((int)(tempShape[0]) + 32);
				string shape = lowerCaseLetter.ToString() + tempShape.Substring(1);

				stringBuilder.AppendLine(string.Format(BounceMethodBody, tempShape.Substring(0, tempShape.Length - 1), shape.Substring(0, shape.Length - 1)));
			}


            // do the shape collections
            string shapeCollectionString = "ShapeCollection";
            string shapeCollectionLowercase = "shapeCollection";
            stringBuilder.AppendLine(string.Format(MethodBody, shapeCollectionString, shapeCollectionLowercase));
            stringBuilder.AppendLine(string.Format(MoveMethodBody, shapeCollectionString, shapeCollectionLowercase));
            stringBuilder.AppendLine(string.Format(BounceMethodBody, shapeCollectionString, shapeCollectionLowercase));



			stringBuilder.AppendLine("		#endregion");
			stringBuilder.AppendLine("*/");


		}

		#endregion
	}
}
