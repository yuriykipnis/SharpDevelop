﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Debugger.Interop.CorDebug;
using Debugger.Interop.CorSym;
using Debugger.Interop.MetaData;
using Mono.Cecil.Signatures;

namespace Debugger.MetaData
{
	public class DebugMethodInfo: System.Reflection.MethodInfo, IDebugMemberInfo, IOverloadable
	{
		DebugType declaringType;
		MethodProps methodProps;
		
		internal DebugMethodInfo(DebugType declaringType, MethodProps methodProps)
		{
			this.declaringType = declaringType;
			this.methodProps = methodProps;
		}
		
		/// <inheritdoc/>
		public override Type DeclaringType {
			get { return declaringType; }
		}
		
		/// <summary> The AppDomain in which this member is declared </summary>
		public AppDomain AppDomain {
			get { return declaringType.AppDomain; }
		}
		
		/// <summary> The Process in which this member is declared </summary>
		public Process Process {
			get { return declaringType.Process; }
		}
		
		/// <summary> The Module in which this member is declared </summary>
		public Debugger.Module DebugModule {
			get { return declaringType.DebugModule; }
		}
		
		/// <inheritdoc/>
		public override int MetadataToken {
			get { return (int)methodProps.Token; }
		}
		
		/// <inheritdoc/>
		public override System.Reflection.Module Module {
			get { throw new NotSupportedException(); }
		}
		
		/// <summary> Name including the declaring type and parameters </summary>
		public string FullName {
			get {
				StringBuilder sb = new StringBuilder();
				sb.Append(this.DeclaringType.FullName);
				sb.Append(".");
				sb.Append(this.Name);
				sb.Append("(");
				bool first = true;
				foreach(DebugParameterInfo p in GetParameters()) {
					if (!first)
						sb.Append(", ");
					first = false;
					sb.Append(p.ParameterType.Name);
					sb.Append(" ");
					sb.Append(p.Name);
				}
				sb.Append(")");
				return sb.ToString();
			}
		}
		
		/// <inheritdoc/>
		public override string Name {
			get { return methodProps.Name; }
		}
		
		/// <inheritdoc/>
		public override Type ReflectedType {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return DebugType.IsDefined(this, inherit, attributeType);
		}
		
		//		public virtual Type[] GetGenericArguments();
		//		public virtual MethodBody GetMethodBody();
		
		/// <inheritdoc/>
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return (MethodImplAttributes)methodProps.ImplFlags;
		}
		
		/// <inheritdoc/>
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			List<Value> args = new List<Value>();
			foreach(object arg in parameters) {
				args.Add((Value)arg);
			}
			if (this.IsSpecialName && this.Name == ".ctor") {
				if (obj != null)
					throw new GetValueException("'obj' must be null for constructor call");
				return Eval.NewObject(this, args.ToArray());
			} else {
				return Eval.InvokeMethod(this, (Value)obj, args.ToArray());
			}
		}
		
		/// <inheritdoc/>
		public override MethodAttributes Attributes {
			get { return (MethodAttributes)methodProps.Flags; }
		}
		
		//		public virtual CallingConventions CallingConvention { get; }
		
		/// <inheritdoc/>
		public override bool ContainsGenericParameters {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override bool IsGenericMethod {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override bool IsGenericMethodDefinition {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override RuntimeMethodHandle MethodHandle {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override MethodInfo GetBaseDefinition()
		{
			throw new NotSupportedException();
		}
		
		//		public override Type[] GetGenericArguments();
		//		public virtual MethodInfo GetGenericMethodDefinition();
		//		public virtual MethodInfo MakeGenericMethod(params Type[] typeArguments);
		//		public override bool ContainsGenericParameters { get; }
		
		/// <inheritdoc/>
		public override Type ReturnType {
			get {
				if (this.MethodDefSig.RetType.Void) return null;
				return DebugType.CreateFromSignature(this.DebugModule, this.MethodDefSig.RetType.Type, declaringType);
			}
		}
		
		/// <inheritdoc/>
		public override ParameterInfo ReturnParameter {
			get {
				if (this.MethodDefSig.RetType.Void) return null;
				return new DebugParameterInfo(this, string.Empty, this.ReturnType, -1, delegate { throw new NotSupportedException(); });
			}
		}
		
		/// <inheritdoc/>
		public override ICustomAttributeProvider ReturnTypeCustomAttributes {
			get { throw new NotSupportedException(); }
		}
		
		MethodDefSig methodDefSig;
		
		MethodDefSig MethodDefSig {
			get {
				if (methodDefSig == null) {
					SignatureReader sigReader = new SignatureReader(methodProps.SigBlob.GetData());
					methodDefSig = sigReader.GetMethodDefSig(0);
				}
				return methodDefSig;
			}
		}
		
		/// <summary> Gets the number of paramters of this method </summary>
		public int ParameterCount {
			get { return this.MethodDefSig.ParamCount; }
		}
		
		ParameterInfo[] parameters;
		
		public DebugParameterInfo GetParameter(string name)
		{
			foreach(DebugParameterInfo par in GetParameters()) {
				if (par.Name == name)
					return par;
			}
			return null;
		}
		
		/// <inheritdoc/>
		public override ParameterInfo[] GetParameters()
		{
			if (parameters == null) {
				parameters = new ParameterInfo[this.MethodDefSig.ParamCount];
				for(int i = 0; i < parameters.Length; i++) {
					string name;
					try {
						// index = 0 is return parameter
						name = this.DebugModule.MetaData.GetParamPropsForMethodIndex((uint)this.MetadataToken, (uint)i + 1).Name;
					} catch {
						name = String.Empty;
					}
					int iCopy = i;
					parameters[i] =
						new DebugParameterInfo(
							this,
							name,
							DebugType.CreateFromSignature(this.DebugModule, this.MethodDefSig.Parameters[i].Type, declaringType),
							i,
							delegate (StackFrame context) { return context.GetArgumentValue(iCopy); }
						);
				}
			}
			return parameters;
		}
		
		internal ICorDebugFunction CorFunction {
			get {
				return this.DebugModule.CorModule.GetFunctionFromToken((uint)this.MetadataToken);
			}
		}
		
		/// <summary> Gets value indicating whether this method should be stepped over
		/// accoring to current options </summary>
		public bool StepOver {
			get {
				Options opt = this.Process.Options;
				if (opt.StepOverNoSymbols) {
					if (this.SymMethod == null) return true;
				}
				if (opt.StepOverDebuggerAttributes) {
					if (this.HasDebuggerAttribute) return true;
				}
				if (opt.StepOverAllProperties) {
					if (this.IsPropertyAccessor) return true;
				}
				if (opt.StepOverSingleLineProperties) {
					if (this.IsPropertyAccessor && this.IsSingleLine) return true;
				}
				if (opt.StepOverFieldAccessProperties) {
					if (this.IsPropertyAccessor && this.BackingField != null) return true;
				}
				return false;
			}
		}
		
		internal bool IsPropertyAccessor { get; set; }
		
		DebugFieldInfo backingFieldCache;
		bool getBackingFieldCalled;
		
		/// <summary>
		/// Backing field that can be used to obtain the same value as by calling this method.
		/// </summary>
		public DebugFieldInfo BackingField {
			get {
				if (!getBackingFieldCalled) {
					backingFieldCache = GetBackingField();
					getBackingFieldCalled = true;
				}
				return backingFieldCache;
			}
		}
		
		// Is this method in form 'return this.field;'?
		DebugFieldInfo GetBackingField()
		{
			if (this.ParameterCount != 0) return null;
			
			ICorDebugCode corCode;
			try {
				corCode = this.CorFunction.GetILCode();
			} catch {
				return null;
			}
			
			if (corCode == null) return null;
			if (corCode.IsIL() == 0) return null;
			if (corCode.GetSize() > 12) return null;
			
			List<byte> code = new List<byte>(corCode.GetCode());
			
			uint token = 0;
			
			bool success =
				(Read(code, 0x00) || true) &&                     // nop || nothing 
				(Read(code, 0x02, 0x7B) || Read(code, 0x7E)) &&   // ldarg.0; ldfld || ldsfld
				ReadToken(code, ref token) &&                     //   <field token>
				(Read(code, 0x0A, 0x2B, 0x00, 0x06) || true) &&   // stloc.0; br.s; offset+00; ldloc.0 || nothing
				Read(code, 0x2A);                                 // ret
		
			if (!success) return null;
			
			MemberInfo member = declaringType.GetMember(token);
			
			if (member == null) return null;
			if (!(member is DebugFieldInfo)) return null;
			
			if (this.Process.Options.Verbose) {
				this.Process.TraceMessage(string.Format("Found backing field for {0}: {1}", this.FullName, member.Name));
			}
			return (DebugFieldInfo)member;
		}
		
		// Read expected sequence of bytes
		static bool Read(List<byte> code, params byte[] expected)
		{
			if (code.Count < expected.Length)
				return false;
			for(int i = 0; i < expected.Length; i++) {
				if (code[i] != expected[i])
					return false;
			}
			code.RemoveRange(0, expected.Length);
			return true;
		}
		
		// Read field token
		static bool ReadToken(List<byte> code, ref uint token)
		{
			if (code.Count < 4)
				return false;
			if (code[3] != 0x04) // field token
				return false;
			token = ((uint)code[0]) + ((uint)code[1] << 8) + ((uint)code[2] << 16) + ((uint)code[3] << 24);
			code.RemoveRange(0, 4);
			return true;
		}
		
		bool? isSingleLine;
		
		bool IsSingleLine {
			get {
				// Note symbols might get loaded manually later by the user
				ISymUnmanagedMethod symMethod = this.SymMethod;
				if (symMethod == null) return false; // No symbols - can not determine
				
				if (isSingleLine.HasValue) return isSingleLine.Value;
				
				List<SequencePoint> seqPoints = new List<SequencePoint>(symMethod.GetSequencePoints());
				seqPoints.Sort();
				
				// Remove initial "{"
				if (seqPoints.Count > 0 &&
				    seqPoints[0].Line == seqPoints[0].EndLine &&
				    seqPoints[0].EndColumn - seqPoints[0].Column <= 1) {
					seqPoints.RemoveAt(0);
				}
				
				// Remove last "}"
				int listIndex = seqPoints.Count - 1;
				if (seqPoints.Count > 0 &&
				    seqPoints[listIndex].Line == seqPoints[listIndex].EndLine &&
				    seqPoints[listIndex].EndColumn - seqPoints[listIndex].Column <= 1) {
					seqPoints.RemoveAt(listIndex);
				}
				
				// Is single line
				isSingleLine = seqPoints.Count == 0 || seqPoints[0].Line == seqPoints[seqPoints.Count - 1].EndLine;
				return isSingleLine.Value;
			}
		}
		
		bool? hasDebuggerAttribute;
		
		bool HasDebuggerAttribute {
			get {
				if (hasDebuggerAttribute.HasValue) return hasDebuggerAttribute.Value;
				
				hasDebuggerAttribute =
					// Look on the method
					DebugType.IsDefined(
						this,
						false,            
						typeof(System.Diagnostics.DebuggerStepThroughAttribute),
						typeof(System.Diagnostics.DebuggerNonUserCodeAttribute),
						typeof(System.Diagnostics.DebuggerHiddenAttribute))
					||
					// Look on the type
					DebugType.IsDefined(
						declaringType,
						false,            
						typeof(System.Diagnostics.DebuggerStepThroughAttribute),
						typeof(System.Diagnostics.DebuggerNonUserCodeAttribute),
						typeof(System.Diagnostics.DebuggerHiddenAttribute));
				
				return hasDebuggerAttribute.Value;
			}
		}
		
		internal void MarkAsNonUserCode()
		{
			((ICorDebugFunction2)this.CorFunction).SetJMCStatus(0 /* false */);
			
			if (this.Process.Options.Verbose) {
				this.Process.TraceMessage("Funciton {0} marked as non-user code", this.FullName);
			}
		}
		
		internal ISymUnmanagedMethod SymMethod {
			get {
				if (this.DebugModule.SymReader == null) return null;
				try {
					return this.DebugModule.SymReader.GetMethod((uint)this.MetadataToken);
				} catch {
					return null;
				}
			}
		}
		
		public DebugLocalVariableInfo GetLocalVariable(string name)
		{
			foreach(DebugLocalVariableInfo loc in GetLocalVariables()) {
				if (loc.Name == name)
					return loc;
			}
			return null;
		}
		
		List<DebugLocalVariableInfo> localVariables;
		
		public List<DebugLocalVariableInfo> GetLocalVariables()
		{
			// Generated constructor may not have any symbols
			if (this.SymMethod == null)
				return new List<DebugLocalVariableInfo>();
					
			if (localVariables != null) return localVariables;
			
			localVariables = GetLocalVariablesInScope(this.SymMethod.GetRootScope());
			if (declaringType.IsDisplayClass || declaringType.IsYieldEnumerator) {
				// Get display class from self
				AddCapturedLocalVariables(
					localVariables,
					delegate(StackFrame context) {
						return context.GetThisValue();
					},
					declaringType
				);
				// Get dispaly classes from fields
				foreach(DebugFieldInfo fieldInfo in this.DeclaringType.GetFields()) {
					DebugFieldInfo fieldInfoCopy = fieldInfo;
					if (fieldInfo.Name.StartsWith("CS$")) {
						AddCapturedLocalVariables(
							localVariables,
							delegate(StackFrame context) {
								return context.GetThisValue().GetFieldValue(fieldInfoCopy);
							},
							(DebugType)fieldInfo.FieldType
						);
					}
				}
			} else {
				// Add this
				if (!this.IsStatic) {
					DebugLocalVariableInfo thisVar = new DebugLocalVariableInfo(
						"this",
						-1,
						declaringType,
						delegate(StackFrame context) {
							return context.GetThisValue();
						}
					);
					thisVar.IsThis = true;
					localVariables.Add(thisVar);
				}
			}
			return localVariables;
		}
		
		static void AddCapturedLocalVariables(List<DebugLocalVariableInfo> vars, ValueGetter getCaptureClass, DebugType captureClassType)
		{
			if (captureClassType.IsDisplayClass || captureClassType.IsYieldEnumerator) {
				foreach(DebugFieldInfo fieldInfo in captureClassType.GetFields()) {
					DebugFieldInfo fieldInfoCopy = fieldInfo;
					if (fieldInfo.Name.StartsWith("CS$")) continue; // Ignore
					DebugLocalVariableInfo locVar = new DebugLocalVariableInfo(
						fieldInfo.Name,
						-1,
						(DebugType)fieldInfo.FieldType,
						delegate(StackFrame context) {
							return getCaptureClass(context).GetFieldValue(fieldInfoCopy);
						}
					);
					locVar.IsCaptured = true;
					if (locVar.Name.StartsWith("<>")) {
						bool hasThis = false;
						foreach(DebugLocalVariableInfo l in vars) {
							if (l.IsThis) {
								hasThis = true;
								break;
							}
						}
						if (!hasThis && locVar.Name.EndsWith("__this")) {
							locVar.Name = "this";
							locVar.IsThis = true;
						} else {
							continue; // Ignore
						}
					}
					if (locVar.Name.StartsWith("<")) {
						int endIndex = locVar.Name.IndexOf('>');
						if (endIndex == -1) continue; // Ignore
						locVar.Name = fieldInfo.Name.Substring(1, endIndex - 1);
					}
					vars.Add(locVar);
				}
			}
		}
		
		List<DebugLocalVariableInfo> GetLocalVariablesInScope(ISymUnmanagedScope symScope)
		{
			List<DebugLocalVariableInfo> vars = new List<DebugLocalVariableInfo>();
			foreach (ISymUnmanagedVariable symVar in symScope.GetLocals()) {
				ISymUnmanagedVariable symVarCopy = symVar;
				int start;
				SignatureReader sigReader = new SignatureReader(symVar.GetSignature());
				LocalVarSig.LocalVariable locVarSig = sigReader.ReadLocalVariable(sigReader.Blob, 0, out start);
				DebugType locVarType = DebugType.CreateFromSignature(this.DebugModule, locVarSig.Type, declaringType);
				// Compiler generated?
				// NB: Display class does not have the compiler-generated flag
				if ((symVar.GetAttributes() & 1) == 1 || symVar.GetName().StartsWith("CS$")) {
					// Get display class from local variable
					if (locVarType.IsDisplayClass) {
						AddCapturedLocalVariables(
							vars,
							delegate(StackFrame context) {
								return GetLocalVariableValue(context, symVarCopy);
							},
							locVarType
						);
					}
				} else {
					DebugLocalVariableInfo locVar = new DebugLocalVariableInfo(
						symVar.GetName(),
						(int)symVar.GetAddressField1(),
						locVarType,
						delegate(StackFrame context) {
							return GetLocalVariableValue(context, symVarCopy);
						}
					);
					vars.Add(locVar);
				}
			}
			foreach(ISymUnmanagedScope childScope in symScope.GetChildren()) {
				vars.AddRange(GetLocalVariablesInScope(childScope));
			}
			return vars;
		}
		
		static Value GetLocalVariableValue(StackFrame context, ISymUnmanagedVariable symVar)
		{
			ICorDebugValue corVal;
			try {
				corVal = context.CorILFrame.GetLocalVariable((uint)symVar.GetAddressField1());
			} catch (COMException e) {
				if ((uint)e.ErrorCode == 0x80131304) throw new GetValueException("Unavailable in optimized code");
				throw;
			}
			return new Value(context.AppDomain, corVal);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			string txt = string.Empty;
			if (this.IsStatic)
				txt += "static ";
			if (this.ReturnType != null) {
				txt += this.ReturnType.FullName + " ";
			} else {
				txt += "void ";
			}
			txt += this.FullName;
			return txt;
		}
		
		IntPtr IOverloadable.GetSignarture()
		{
			return methodProps.SigBlob.Adress;
		}
		
		DebugType IDebugMemberInfo.MemberType {
			get { return (DebugType)this.ReturnType; }
		}
	}
}