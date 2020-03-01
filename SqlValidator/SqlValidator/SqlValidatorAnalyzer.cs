using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlValidator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SqlValidatorAnalyzer : DiagnosticAnalyzer
    {
        private class Param
        {
            public string Name { get; set; }
            public Location Location { get; set; }
            public object Value { get; set; }
        }

        public const string DiagnosticId = "SqlCSharpAnalyzer";

        private static readonly LocalizableString Title = "SQL Validation";
        private static readonly LocalizableString MessageFormat = "{0}";
        private static readonly LocalizableString Description = string.Empty;

        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor _rule =
            new DiagnosticDescriptor(
                DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
                isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;
            var sqlClientType = context.Compilation.GetTypeByMetadataName("System.Data.SqlClient.SqlCommand");
            if (sqlClientType is null) return;

            var siblings = node.FirstAncestorOrSelf<BlockSyntax>()
                .DescendantNodes();

            var symbol = context.SemanticModel.GetSymbolInfo(node);
            if (symbol.Symbol?.ContainingType != sqlClientType) return;

            var parameters = GetSqlParams(context, node, siblings);

            var sqlParam = node.ArgumentList.Arguments[0].Expression;
            var sql = sqlParam.GetConstantValue(context.Compilation);

            if (sql is null) return;

            foreach (var param in parameters)
            {
                if (sql.Contains($"@{param.Name}") == false)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(_rule, param.Location, $"Unused parameter: {param.Name}"));
                }
            }

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("SqlValidator_ConnectionString", EnvironmentVariableTarget.User);
                if (connectionString is null) return;

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Name, param.Value);
                        }

                        var reader = command.ExecuteReader(System.Data.CommandBehavior.SchemaOnly);
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, node.GetLocation(), ex.Message));
            }
        }

        private static List<Param> GetSqlParams(
            SyntaxNodeAnalysisContext context,
            ObjectCreationExpressionSyntax node,
            IEnumerable<SyntaxNode> siblings)
        {
            var parameters = new List<Param>();

            var variableDeclarator = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            if (variableDeclarator is null)
            {
                return parameters;
            }

            var variableSymbol = context.SemanticModel.GetDeclaredSymbol(variableDeclarator);

            foreach (var sibling in siblings)
            {
                // command.Parameters.AddWithValue
                if (sibling is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax addWithValue &&
                    addWithValue.Name.ToString() == "AddWithValue" &&
                    addWithValue.Expression is MemberAccessExpressionSyntax paramsExpr &&
                    paramsExpr.Name.ToString() == "Parameters" &&
                    paramsExpr.Expression is IdentifierNameSyntax command &&
                    context.SemanticModel.GetSymbolInfo(command).Symbol == variableSymbol)
                {
                    var paramExpression = invocation.ArgumentList.Arguments[0].Expression;
                    var paramName = paramExpression.GetConstantValue(context.Compilation);

                    parameters.Add(new Param
                    {
                        Name = paramName,
                        Location = invocation.GetLocation(),
                        Value = paramExpression.GetDummyValue(context.SemanticModel)
                    });
                }
            }

            return parameters;
        }
    }
}
