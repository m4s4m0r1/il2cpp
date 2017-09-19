﻿using System.Collections.Generic;
using System.Linq;

namespace il2cpp
{
	internal class CompileUnit
	{
		public string Name;
		public string DeclCode;
		public string ImplCode;
		public readonly HashSet<string> DeclDepends = new HashSet<string>();
		public readonly HashSet<string> ImplDepends = new HashSet<string>();
	}

	internal class TypeGenerator
	{
		private readonly GeneratorContext GenContext;
		private readonly TypeX CurrType;

		public TypeGenerator(GeneratorContext genContext, TypeX tyX)
		{
			GenContext = genContext;
			CurrType = tyX;
		}

		public CompileUnit Generate()
		{
			CompileUnit unit = new CompileUnit();
			unit.Name = GenContext.GetTypeName(CurrType);

			// 生成类结构
			CodePrinter prtDecl = new CodePrinter();

			prtDecl.AppendFormatLine("// {0}", CurrType.GetNameKey());
			if (CurrType.BaseType != null)
			{
				prtDecl.AppendFormatLine("struct {0} : {1}",
					GenContext.GetTypeName(CurrType),
					GenContext.GetTypeName(CurrType.BaseType));
			}
			else
			{
				prtDecl.AppendFormatLine("struct {0}",
					GenContext.GetTypeName(CurrType));
			}

			prtDecl.AppendLine("{");
			++prtDecl.Indents;

			// 重排字段
			var fields = LayoutFields();
			// 生成字段
			foreach (var fldX in fields)
			{
				prtDecl.AppendFormatLine("{0} {1};",
					GenContext.GetTypeName(fldX.FieldType),
					GenContext.GetFieldName(fldX));
			}

			--prtDecl.Indents;
			prtDecl.AppendLine("};");

			CodePrinter prtImpl = new CodePrinter();

			// 生成方法
			foreach (MethodX metX in CurrType.Methods)
			{
				var metGen = new MethodGenerator(GenContext, metX);
				metGen.Generate();

				prtDecl.AppendFormatLine("// {0}", metX.GetNameKey());
				prtDecl.AppendLine(metGen.DeclCode);
				unit.DeclDepends.UnionWith(metGen.DeclDepends);

				prtImpl.Append(metGen.ImplCode);
				unit.ImplDepends.UnionWith(metGen.ImplDepends);
			}

			unit.DeclCode = prtDecl.ToString();
			unit.ImplCode = prtImpl.ToString();

			return unit;
		}

		private List<FieldX> LayoutFields()
		{
			var fields = CurrType.Fields.ToList();
			fields.Sort((lhs, rhs) =>
				GenContext.GetTypeLayoutOrder(lhs.FieldType) - GenContext.GetTypeLayoutOrder(rhs.FieldType));
			return fields;
		}
	}
}
