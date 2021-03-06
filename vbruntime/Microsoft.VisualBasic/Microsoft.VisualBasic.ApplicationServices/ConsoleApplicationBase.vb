'
' Microsoft.VisualBasic.ApplicationServices.ConsoleApplicationBase.vb
'
' Authors:
'   Miguel de Icaza (miguel@novell.com)
'   Mizrahi Rafael (rafim@mainsoft.com)
'   Rolf Bjarne Kvinge  (RKvinge@novell.com)
'
' Copyright (C) 2006-2007 Novell (http://www.novell.com)
'
' Permission is hereby granted, free of charge, to any person obtaining
' a copy of this software and associated documentation files (the
' "Software"), to deal in the Software without restriction, including
' without limitation the rights to use, copy, modify, merge, publish,
' distribute, sublicense, and/or sell copies of the Software, and to
' permit persons to whom the Software is furnished to do so, subject to
' the following conditions:
' 
' The above copyright notice and this permission notice shall be
' included in all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
' EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
' MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
' NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
' LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
' OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
' WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
'

Imports System
Imports System.Threading
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
#If mono_not_yet Then
Imports System.Deployment.Application
#End If

Namespace Microsoft.VisualBasic.ApplicationServices
    Public Class ConsoleApplicationBase
        Inherits ApplicationBase

        Public Sub New()
            MyBase.New()
        End Sub

        Public ReadOnly Property CommandLineArgs() As ReadOnlyCollection(Of String)
            Get
                Dim CommandLineStrings() As String = Environment.GetCommandLineArgs()

                Dim ReturnCollection As New List(Of String)(CommandLineStrings)

                ' Do not return the application name, just the command line args
                If ReturnCollection.Count > 0 Then
                    ReturnCollection.RemoveAt(0)
                End If

                Return ReturnCollection.AsReadOnly()
            End Get
        End Property
#If mono_not_yet Then
        Public ReadOnly Property Deployment() As ApplicationDeployment
            Get
                Throw New NotImplementedException
            End Get
        End Property
#End If
        <EditorBrowsable(EditorBrowsableState.Advanced)> _
        Protected WriteOnly Property InternalCommandLine() As ReadOnlyCollection(Of String)
            Set(ByVal value As ReadOnlyCollection(Of String))
                Throw New NotImplementedException
            End Set
        End Property

        Public ReadOnly Property IsNetworkDeployed() As Boolean
            Get
                Throw New NotImplementedException
            End Get
        End Property
    End Class
End Namespace

