using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Managers;
using GlueViewOfficialPlugins.Scripting;
using ICSharpCode.NRefactory.CSharp;

namespace GlueView.Scripting
{
    public class BranchingParser : Singleton<BranchingParser>
    {
        public bool DetermineIfShouldExecute(
            ScriptParsingPlugin plugin,
            CodeContext codeContext, 
            ConditionalCodeBlock ccb, 
            ExpressionParser expressionParser,
            bool isFirstExecution)
        {
            bool shouldExecute = false;

            if (ccb.BlockType == BlockType.Else)
            {
                shouldExecute = true;
            }
            else if (ccb.BlockType == BlockType.For)
            {
                Statement[] statements = new CSharpParser().ParseStatements(ccb.ConditionExpression) as Statement[];

                if (isFirstExecution)
                {
                    VariableDeclarationStatement declaration = statements[0] as VariableDeclarationStatement;

                    var variable = declaration.Variables.FirstOrDefault();

                    plugin.ApplyAssignment(variable, declaration, codeContext);
                }

                var predicateExpression = 
                    (statements[1] as ExpressionStatement).Expression;
                var resultAsObject = expressionParser.EvaluateExpression(predicateExpression, codeContext);
                return (bool)resultAsObject;
            }
            else if (ccb.BlockType == BlockType.While)
            {
                shouldExecute =
                 (bool)expressionParser.EvaluateExpression(ccb.ConditionExpression, codeContext);
             
            }
            else
            {
                try
                {
                    shouldExecute =
                        (bool)expressionParser.EvaluateExpression(ccb.ConditionExpression, codeContext);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return shouldExecute;
        }


        public void IncrementFor(CodeContext codeContext, ConditionalCodeBlock ccb, ExpressionParser expressionParser)
        {
            Statement[] statements = new CSharpParser().ParseStatements(ccb.ConditionExpression) as Statement[];

            ExpressionStatement incrementStatement = statements[2] as ExpressionStatement;

            expressionParser.EvaluateExpression(incrementStatement.Expression, codeContext);
        }

    }
}
