' 
' Visual Basic.Net Compiler
' Copyright (C) 2004 - 2008 Rolf Bjarne Kvinge, RKvinge@novell.com
' 
' This library is free software; you can redistribute it and/or
' modify it under the terms of the GNU Lesser General Public
' License as published by the Free Software Foundation; either
' version 2.1 of the License, or (at your option) any later version.
' 
' This library is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
' Lesser General Public License for more details.
' 
' You should have received a copy of the GNU Lesser General Public
' License along with this library; if not, write to the Free Software
' Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
' 
#If DEBUG Then
#Const EXTENDEDDEBUG = 0
#End If
''' <summary>
''' TypeDeclaration Hierarchy:
''' 
''' TypeDeclaration
'''   + DelegateDeclaration
'''   + EnumDeclaration
'''   + ModuleDeclaration
'''   - GenericTypeDeclaration
'''       + InterfaceDeclaration
'''       - PartialTypeDeclaration
'''           + ClassDeclaration
'''           + StructureDeclaration
'''         
''' </summary>
''' <remarks></remarks>
Public MustInherit Class TypeDeclaration
    Inherits MemberDeclaration
    Implements IType

    'Private m_TypeDescriptor As TypeDescriptor

    'Information collected during parse phase.
    Private m_Members As New MemberDeclarations(Me)
    Private m_Namespace As String
    Private m_Name As Identifier

    Private m_DefaultInstanceConstructor As ConstructorDeclaration
    Private m_DefaultSharedConstructor As ConstructorDeclaration
    Private m_StaticVariables As Generic.List(Of LocalVariableDeclaration)
    Private m_BeforeFieldInit As Boolean
    Private m_Serializable As Boolean

    'Information collected during define phase.
    Private m_CecilType As Mono.Cecil.TypeDefinition

    Private m_FullName As String

    Private m_AddHandlers As New Generic.List(Of AddOrRemoveHandlerStatement)
    Private m_MyGroupField As TypeVariableDeclaration

    Property MyGroupField() As TypeVariableDeclaration
        Get
            Return m_MyGroupField
        End Get
        Set(ByVal value As TypeVariableDeclaration)
            m_MyGroupField = value
        End Set
    End Property

    Property Serializable() As Boolean
        Get
            Return m_Serializable
        End Get
        Set(ByVal value As Boolean)
            If m_CecilType IsNot Nothing Then m_CecilType.IsSerializable = value
            m_Serializable = value
        End Set
    End Property

    Public Overrides Sub Initialize(ByVal Parent As BaseObject)
        MyBase.Initialize(Parent)

        For Each member As MemberDeclaration In m_Members
            member.Initialize(Me)
        Next
    End Sub

    ReadOnly Property DescriptiveType() As String
        Get
            If TypeOf Me Is ClassDeclaration Then
                Return "class"
            ElseIf TypeOf Me Is ModuleDeclaration Then
                Return "module"
            ElseIf TypeOf Me Is EnumDeclaration Then
                Return "enum"
            ElseIf TypeOf Me Is StructureDeclaration Then
                Return "structure"
            ElseIf TypeOf Me Is DelegateDeclaration Then
                Return "delegate"
            ElseIf TypeOf Me Is InterfaceDeclaration Then
                Return "interface"
            Else
                Return "type"
            End If
        End Get
    End Property

    ReadOnly Property AddHandlers() As Generic.List(Of AddOrRemoveHandlerStatement)
        Get
            Return m_AddHandlers
        End Get
    End Property

    Sub New(ByVal Parent As ParsedObject, ByVal [Namespace] As String)
        MyBase.New(Parent)
        'm_TypeDescriptor = New TypeDescriptor(Me)

        m_Namespace = [Namespace]

        Helper.Assert(m_Namespace IsNot Nothing)
        UpdateDefinition()
    End Sub

    Shadows Sub Init(ByVal Name As Identifier, ByVal TypeArgumentCount As Integer)
        MyBase.Init(Helper.CreateGenericTypename(Name.Name, TypeArgumentCount))

        m_Name = Name

        Helper.Assert(DeclaringType IsNot Nothing OrElse TypeOf Me.Parent Is AssemblyDeclaration)
        Helper.Assert(m_Namespace IsNot Nothing)
        'Helper.Assert(m_Name IsNot Nothing)

        UpdateDefinition()

    End Sub

    Sub SetName(ByVal Name As Identifier, ByVal TypeArgumentCount As Integer)
        m_Name = Name
        MyBase.Name = Helper.CreateGenericTypename(Name.Name, TypeArgumentCount)
    End Sub

    Overrides Sub UpdateDefinition()
        MyBase.UpdateDefinition()

        If m_CecilType Is Nothing Then
            If Me.IsNestedType Then
                m_CecilType = New Mono.Cecil.TypeDefinition(Me.Name, Nothing, 0, Nothing)
            Else
                m_CecilType = New Mono.Cecil.TypeDefinition(Me.Name, Me.Namespace, 0, Nothing)
            End If
            m_CecilType.Annotations.Add(Compiler, Me)
        End If
        m_CecilType.Name = Name

        If CecilType.Module Is Nothing Then
            Compiler.ModuleBuilderCecil.Types.Add(CecilType)
            If IsNestedType Then
                DeclaringType.CecilType.NestedTypes.Add(CecilType)
            End If
        End If
    End Sub

    Protected Property BeforeFieldInit() As Boolean
        Get
            Return m_BeforeFieldInit
        End Get
        Set(ByVal value As Boolean)
            m_BeforeFieldInit = value
            UpdateDefinition()
        End Set
    End Property

    Protected Friend Property DefaultInstanceConstructor() As ConstructorDeclaration
        Get
            Return m_DefaultInstanceConstructor
        End Get
        Set(ByVal value As ConstructorDeclaration)
            m_DefaultInstanceConstructor = value
        End Set
    End Property

    Protected Property DefaultSharedConstructor() As ConstructorDeclaration
        Get
            Return m_DefaultSharedConstructor
        End Get
        Set(ByVal value As ConstructorDeclaration)
            m_DefaultSharedConstructor = value
        End Set
    End Property

    Protected Sub FindDefaultConstructors()
        For i As Integer = 0 To Me.Members.Count - 1
            Dim member As IMember = Me.Members(i)
            Dim ctor As ConstructorDeclaration = TryCast(member, ConstructorDeclaration)

            If ctor Is Nothing Then Continue For

            Dim isdefault As Boolean
            isdefault = False
            If ctor.GetParameters.Length = 0 Then
                isdefault = True
            Else
                isdefault = ctor.GetParameters()(0).IsOptional
            End If
            If isdefault Then
                If ctor.IsShared Then
                    Helper.Assert(m_DefaultSharedConstructor Is Nothing OrElse m_DefaultSharedConstructor Is ctor)
                    m_DefaultSharedConstructor = ctor
                Else
                    Helper.Assert(m_DefaultInstanceConstructor Is Nothing OrElse m_DefaultInstanceConstructor Is ctor)
                    m_DefaultInstanceConstructor = ctor
                End If
            End If
        Next
    End Sub

    ReadOnly Property [Namespace]() As String Implements IType.Namespace
        Get
            Helper.Assert(m_Namespace IsNot Nothing)
            Return m_Namespace
        End Get
    End Property

    Sub AddInterface(ByVal Type As Mono.Cecil.TypeReference)
        m_CecilType.Interfaces.Add(Helper.GetTypeOrTypeReference(Compiler, Type))
    End Sub

    ReadOnly Property Identifier() As Identifier
        Get
            Return m_Name
        End Get
    End Property

    Public Overrides ReadOnly Property IsShared() As Boolean
        Get
            Return TypeOf Me Is ModuleDeclaration
        End Get
    End Property

    Public Overrides ReadOnly Property MemberDescriptor() As Mono.Cecil.MemberReference
        Get
            Return m_CecilType
        End Get
    End Property

    Public Overrides ReadOnly Property FullName() As String
        Get
            If m_FullName Is Nothing Then
                If Me.IsNestedType Then
                    m_FullName = DeclaringType.FullName & "+" & Me.Name
                Else
                    If m_Namespace <> "" Then
                        m_FullName = m_Namespace & "." & Me.Name
                    Else
                        m_FullName = Me.Name
                    End If
                End If
            End If
            Return m_FullName
        End Get
    End Property

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property BaseType() As Mono.Cecil.TypeReference Implements IType.BaseType
        Get
            If m_CecilType Is Nothing Then
                Return Nothing
            End If
            Return m_CecilType.BaseType
        End Get
        Set(ByVal value As Mono.Cecil.TypeReference)
            m_CecilType.BaseType = Helper.GetTypeOrTypeReference(Compiler, value)
        End Set
    End Property

    Public ReadOnly Property Members() As MemberDeclarations Implements IType.Members
        Get
            Return m_Members
        End Get
    End Property

    Public Overridable ReadOnly Property CecilType() As Mono.Cecil.TypeDefinition Implements IType.CecilType
        Get
            Return m_CecilType
        End Get
    End Property

    ReadOnly Property IsNestedType() As Boolean Implements IType.IsNestedType
        Get
            Dim result As Boolean
            result = DeclaringType IsNot Nothing
            Helper.Assert(result = (Me.FindFirstParent(Of IType)() IsNot Nothing))
            Return result
        End Get
    End Property

    Public Overrides Function ResolveTypeReferences() As Boolean
        Dim result As Boolean = True

        result = MyBase.ResolveTypeReferences AndAlso result

        If File IsNot Nothing AndAlso File.IsOptionCompareText Then
            Dim textAttribute As Attribute
            textAttribute = New Attribute(Me, Compiler.TypeCache.MS_VB_CS_OptionTextAttribute)
            CustomAttributes.Add(textAttribute)
        End If

        m_StaticVariables = New Generic.List(Of LocalVariableDeclaration)
        For Each method As MethodDeclaration In m_Members.GetSpecificMembers(Of MethodDeclaration)()
            If method.Code IsNot Nothing Then method.Code.FindStaticVariables(m_StaticVariables)
        Next
        For Each prop As PropertyDeclaration In m_Members.GetSpecificMembers(Of PropertyDeclaration)()
            If prop.GetDeclaration IsNot Nothing AndAlso prop.GetDeclaration.Code IsNot Nothing Then prop.GetDeclaration.Code.FindStaticVariables(m_StaticVariables)
            If prop.SetDeclaration IsNot Nothing AndAlso prop.SetDeclaration.Code IsNot Nothing Then prop.SetDeclaration.Code.FindStaticVariables(m_StaticVariables)
        Next

        Return result
    End Function

    Overridable Function DefineType() As Boolean Implements IDefinableType.DefineType
        Dim result As Boolean = True

        Helper.Assert(BaseType IsNot Nothing OrElse Me.CecilType.IsInterface)

        Return result
    End Function

    Public Overridable Function DefineTypeHierarchy() As Boolean Implements IDefinableType.DefineTypeHierarchy
        Dim result As Boolean = True

        If BaseType Is Nothing Then
            If Me.IsInterface = False Then
                m_CecilType.BaseType = Helper.GetTypeOrTypeReference(Compiler, Compiler.TypeCache.System_Void)
            End If
        End If
        If DeclaringType IsNot Nothing Then
            m_CecilType.DeclaringType = DeclaringType.CecilType
        End If

        Return result
    End Function

    Public Function SetDefaultAttribute(ByVal Name As String) As Boolean
        Dim result As Boolean = True
        For Each att As Attribute In CustomAttributes
            result = att.ResolveCode(ResolveInfo.Default(Compiler)) AndAlso result
            If result = False Then Return result
            If Helper.CompareType(att.AttributeType, Compiler.TypeCache.System_Reflection_DefaultMemberAttribute) Then
                Dim tmpName As String
                tmpName = TryCast(att.GetArgument(0), String)
                If tmpName IsNot Nothing AndAlso Helper.CompareNameOrdinal(Name, tmpName) = False Then
                    Compiler.Report.ShowMessage(Messages.VBNC32304, Location, Me.FullName, tmpName, Name)
                    Return False
                End If
                Return True
            End If
        Next

        'Helper.NotImplementedYet("Check that the property is indexed.")
        Dim attrib As Attribute
        attrib = New Attribute(Me, Compiler.TypeCache.System_Reflection_DefaultMemberAttribute, Name)
        result = attrib.ResolveCode(ResolveInfo.Default(Compiler)) AndAlso result
        CustomAttributes.Add(attrib)
        Return result
    End Function

    Public Overrides Function ResolveCode(ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True

        result = MyBase.ResolveCode(Info) AndAlso result
        Compiler.VerifyConsistency(result, Location)
        result = m_Members.ResolveCode(Info) AndAlso result
        'vbnc.Helper.Assert(result = (Compiler.Report.Errors = 0))

        Return result
    End Function

    Friend Overrides Function GenerateCode(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True

        result = MyBase.GenerateCode(Info) AndAlso result

        Return result
    End Function

    ReadOnly Property StaticVariables() As Generic.List(Of LocalVariableDeclaration)
        Get
            Return m_StaticVariables
        End Get
    End Property

    Property TypeAttributes() As Mono.Cecil.TypeAttributes Implements IType.TypeAttributes
        Get
            Return m_CecilType.Attributes
        End Get
        Set(ByVal value As Mono.Cecil.TypeAttributes)
            m_CecilType.Attributes = value
            m_CecilType.IsBeforeFieldInit = m_BeforeFieldInit
            m_CecilType.IsSerializable = m_Serializable
        End Set
    End Property

    ReadOnly Property IsInterface() As Boolean
        Get
            Return TypeOf Me Is InterfaceDeclaration
        End Get
    End Property

    ReadOnly Property IsClass() As Boolean
        Get
            Return TypeOf Me Is ClassDeclaration
        End Get
    End Property

    ReadOnly Property IsDelegate() As Boolean
        Get
            Return TypeOf Me Is DelegateDeclaration
        End Get
    End Property

    ReadOnly Property IsValueType() As Boolean
        Get
            Return TypeOf Me Is StructureDeclaration
        End Get
    End Property

    ReadOnly Property IsEnum() As Boolean
        Get
            Return TypeOf Me Is EnumDeclaration
        End Get
    End Property

    ReadOnly Property IsModule() As Boolean
        Get
            Return TypeOf Me Is ModuleDeclaration
        End Get
    End Property

    ReadOnly Property HasInstanceConstructors() As Boolean
        Get
            Dim ctors As Generic.List(Of ConstructorDeclaration)
            ctors = Me.Members.GetSpecificMembers(Of ConstructorDeclaration)()
            For Each item As ConstructorDeclaration In ctors
                If item.IsShared = False Then Return True
            Next
            Return False
        End Get
    End Property

    ReadOnly Property HasSharedConstantFields() As Boolean
        Get
            Dim ctors As Generic.List(Of ConstantDeclaration)
            ctors = Me.Members.GetSpecificMembers(Of ConstantDeclaration)()
            Return ctors.Count > 0
        End Get
    End Property

    ReadOnly Property HasSharedFieldsWithInitializers() As Boolean
        Get
            Dim ctors As Generic.List(Of VariableDeclaration)
            ctors = Me.Members.GetSpecificMembers(Of VariableDeclaration)()
            For Each item As VariableDeclaration In ctors
                If item.IsShared AndAlso item.HasInitializer Then Return True
            Next
            For Each item As VariableDeclaration In m_StaticVariables
                If item.DeclaringMethod.IsShared AndAlso item.HasInitializer Then Return True
            Next
            For Each item As ConstantDeclaration In Members.GetSpecificMembers(Of ConstantDeclaration)()
                If item.RequiresSharedInitialization Then Return True
            Next
            Return False
        End Get
    End Property

End Class