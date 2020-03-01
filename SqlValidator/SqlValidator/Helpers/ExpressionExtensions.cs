using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public static class ExpressionExtensions
    {
        // https://stackoverflow.com/a/46362016/7003797
        public static string GetConstantValue(this ExpressionSyntax expression, Compilation compilation)
        {
            if (expression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var literal = expression as LiteralExpressionSyntax;
                return literal.Token.ValueText;
            }
            else if (expression.IsKind(SyntaxKind.AddExpression))
            {
                var binaryExpression = expression as BinaryExpressionSyntax;
                return GetConstantValue(binaryExpression.Left, compilation) +
                    GetConstantValue(binaryExpression.Right, compilation);
            }
            else
            {
                var model = compilation.GetSemanticModel(expression.SyntaxTree);
                var symbol = model.GetSymbolInfo(expression).Symbol;
                var defNode = symbol.DeclaringSyntaxReferences.First().GetSyntax();

                var valueClause = defNode.DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
                if (valueClause != null)
                {
                    return GetConstantValue(valueClause.Value, compilation);
                }
                else
                {
                    return null;
                }
            }
        }

        public static object GetDummyValue(this ExpressionSyntax expression, Compilation compilation)
        {
            if (expression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var literal = expression as LiteralExpressionSyntax;
                return literal.Token.ValueText;
            }
            else if (expression.IsKind(SyntaxKind.AddExpression))
            {
                var binaryExpression = expression as BinaryExpressionSyntax;
                return GetConstantValue(binaryExpression.Left, compilation) +
                    GetConstantValue(binaryExpression.Right, compilation);
            }
            else
            {
                var model = compilation.GetSemanticModel(expression.SyntaxTree);
                var symbol = model.GetSymbolInfo(expression).Symbol;
                var defNode = symbol.DeclaringSyntaxReferences.First().GetSyntax();

                var valueClause = defNode.DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
                if (valueClause != null)
                {
                    return GetConstantValue(valueClause.Value, compilation);
                }
                else
                {
                    return null;
                }
            }
        }

        public static object GetDummyValue(this ExpressionSyntax paramExpression, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(paramExpression);
            switch (typeInfo.Type.Name)
            {
                case nameof(String):
                    return string.Empty;
                case nameof(Int32):
                    return 0;
                case nameof(Double):
                    return 0d;
                case nameof(Single):
                    return 0f;
                case nameof(Boolean):
                    return false;
            }

            throw new ArgumentOutOfRangeException();
        }

    }
}
