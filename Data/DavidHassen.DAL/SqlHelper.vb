Imports System
Imports System.Data
Imports System.Xml
Imports System.Data.SqlClient
Imports System.Collections
Imports System.Configuration


''' <summary>
''' The SqlHelper class is intended to encapsulate high performance, scalable best practices for 
''' common uses of SqlClient
''' </summary>
Public NotInheritable Class SqlHelper
#Region "Private utility methods & constructors"

    Private Sub New()
        MyBase.New

    End Sub

    ' Connection string 
    Public Shared Function GetConnectionString() As String
        'return Shared.Utils.Setting.FREYContext"];
        'return System.Configuration.ConnectionStrings["FREYContext"].ConnectionString;
        Return System.Configuration.ConfigurationManager.ConnectionStrings("ConnectionString").ConnectionString
    End Function

    ''' <summary>
    ''' This method is used to attach array of SqlParameters to a SqlCommand.
    ''' 
    ''' This method will assign a value of DbNull to any parameter with a direction of
    ''' InputOutput and a value of null.  
    ''' 
    ''' This behavior will prevent default values from being used, but
    ''' this will be the less common case than an intended pure output parameter (derived as InputOutput)
    ''' where the user provided no input value.
    ''' </summary>
    ''' <param name="command">The command to which the parameters will be added</param>
    ''' <param name="commandParameters">An array of SqlParameters to be added to command</param>
    Private Shared Sub AttachParameters(ByVal command As SqlCommand, ByVal commandParameters() As SqlParameter)
        If (command Is Nothing) Then
            Throw New ArgumentNullException("command")
        End If

        If (Not (commandParameters) Is Nothing) Then
            For Each p As SqlParameter In commandParameters
                If (Not (p) Is Nothing) Then
                    ' Check for derived output value with no value assigned
                    If (((p.Direction = ParameterDirection.InputOutput) _
                                OrElse (p.Direction = ParameterDirection.Input)) _
                                AndAlso (p.Value Is Nothing)) Then
                        p.Value = DBNull.Value
                    End If

                    command.Parameters.Add(p)
                End If

            Next
        End If

    End Sub

    'function is used to return data in datatable 
    Public Shared Function FillDataTable(ByVal Sql As String) As DataTable
        Try
            Return SqlHelper.ExecuteDataset(SqlHelper.GetConnectionString, CommandType.Text, Sql).Tables(0)
        Catch ex As Exception
            Throw ex
        End Try

    End Function

    Public Shared Function FillDataReader(ByVal sql As String) As SqlDataReader
        Try
            Return SqlHelper.ExecuteReader(SqlHelper.GetConnectionString, CommandType.Text, sql)
        Catch ex As Exception
            Throw ex
        End Try

    End Function

    ''' <summary>
    ''' This method assigns dataRow column values to an array of SqlParameters
    ''' </summary>
    ''' <param name="commandParameters">Array of SqlParameters to be assigned values</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values</param>
    Private Overloads Shared Sub AssignParameterValues(ByVal commandParameters() As SqlParameter, ByVal dataRow As DataRow)
        If ((commandParameters Is Nothing) _
                    OrElse (dataRow Is Nothing)) Then
            ' Do nothing if we get no data
            Return
        End If

        Dim i As Integer = 0
        ' Set the parameters values
        For Each commandParameter As SqlParameter In commandParameters
            ' Check the parameter name
            If ((commandParameter.ParameterName Is Nothing) _
                        OrElse (commandParameter.ParameterName.Length <= 1)) Then
                Throw New Exception(String.Format("Please provide a valid parameter name on the parameter #{0}, the ParameterName property has the follo" &
                        "wing value: '{1}'.", i, commandParameter.ParameterName))
            End If

            If (dataRow.Table.Columns.IndexOf(commandParameter.ParameterName.Substring(1)) <> -1) Then
                commandParameter.Value = dataRow(commandParameter.ParameterName.Substring(1))
            End If

            i = (i + 1)
        Next
    End Sub

    ''' <summary>
    ''' This method assigns an array of values to an array of SqlParameters
    ''' </summary>
    ''' <param name="commandParameters">Array of SqlParameters to be assigned values</param>
    ''' <param name="parameterValues">Array of objects holding the values to be assigned</param>
    Private Overloads Shared Sub AssignParameterValues(ByVal commandParameters() As SqlParameter, ByVal parameterValues() As Object)
        If ((commandParameters Is Nothing) _
                    OrElse (parameterValues Is Nothing)) Then
            ' Do nothing if we get no data
            Return
        End If

        ' We must have the same number of values as we pave parameters to put them in
        If (commandParameters.Length <> parameterValues.Length) Then
            Throw New ArgumentException("Parameter count does not match Parameter Value count.")
        End If

        ' Iterate through the SqlParameters, assigning the values from the corresponding position in the 
        ' value array
        Dim i As Integer = 0
        Dim j As Integer = commandParameters.Length
        Do While (i < j)
            ' If the current array value derives from IDbDataParameter, then assign its Value property
            If (TypeOf parameterValues(i) Is IDbDataParameter) Then
                Dim paramInstance As IDbDataParameter = CType(parameterValues(i), IDbDataParameter)
                If (paramInstance.Value Is Nothing) Then
                    commandParameters(i).Value = DBNull.Value
                Else
                    commandParameters(i).Value = paramInstance.Value
                End If

            ElseIf (parameterValues(i) Is Nothing) Then
                commandParameters(i).Value = DBNull.Value
            Else
                commandParameters(i).Value = parameterValues(i)
            End If

            i = (i + 1)
        Loop

    End Sub

    ''' <summary>
    ''' This method opens (if necessary) and assigns a connection, transaction, command type and parameters 
    ''' to the provided command
    ''' </summary>
    ''' <param name="command">The SqlCommand to be prepared</param>
    ''' <param name="connection">A valid SqlConnection, on which to execute this command</param>
    ''' <param name="transaction">A valid SqlTransaction, or 'null'</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParameters to be associated with the command or 'null' if no parameters are required</param>
    ''' <param name="mustCloseConnection"><c>true</c> if the connection was opened by the method, otherwose is false.</param>
    Private Shared Sub PrepareCommand(ByVal command As SqlCommand, ByVal connection As SqlConnection, ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ByVal commandParameters() As SqlParameter, ByRef mustCloseConnection As Boolean)
        If (command Is Nothing) Then
            Throw New ArgumentNullException("command")
        End If

        If ((commandText Is Nothing) _
                    OrElse (commandText.Length = 0)) Then
            Throw New ArgumentNullException("commandText")
        End If

        If (connection.State <> ConnectionState.Open) Then
            mustCloseConnection = True
            connection.Open()
        Else
            mustCloseConnection = False
        End If

        ' Associate the connection with the command
        command.Connection = connection
        ' Set the command text (stored procedure name or SQL statement)
        command.CommandText = commandText
        ' Set timeout for command
        command.CommandTimeout = 0
        ' If we were provided a transaction, assign it
        If (Not (transaction) Is Nothing) Then
            If (transaction.Connection Is Nothing) Then
                Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
            End If

            command.Transaction = transaction
        End If

        ' Set the command type
        command.CommandType = commandType
        ' Attach the command parameters if they are provided
        If (Not (commandParameters) Is Nothing) Then
            SqlHelper.AttachParameters(command, commandParameters)
        End If

        Return
    End Sub
#End Region
#Region "ExecuteNonQuery"

    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String) As Integer
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteNonQuery(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    'function used to return single value
    Public Shared Function ReturnScalar(ByVal sql As String) As String
        Try
            Return Convert.ToString(SqlHelper.ExecuteScalar(SqlHelper.GetConnectionString, CommandType.Text, sql))
        Catch ex As Exception
            Throw ex
        End Try

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns no resultset) against the database specified in the connection string 
    ''' using the provided parameters
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As Integer
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        Dim connection As SqlConnection = New SqlConnection(connectionString)
        connection.Open()
        ' Call the overload that takes a connection in place of the connection string
        Return SqlHelper.ExecuteNonQuery(connection, commandType, commandText, commandParameters)
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in 
    ''' the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(connString, "PublishOrders", 24, 36);
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="spName">The name of the stored prcedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As Integer
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlConnection. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders");
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String) As Integer
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteNonQuery(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns no resultset) against the specified SqlConnection 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As Integer
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        Dim cmd As SqlCommand = New SqlCommand
        Dim mustCloseConnection As Boolean = False
        SqlHelper.PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, mustCloseConnection)
        ' Finally, execute the command
        Dim retval As Integer = cmd.ExecuteNonQuery
        ' Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        If mustCloseConnection Then
            connection.Close()
        End If

        Return retval
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlConnection 
    ''' using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(conn, "PublishOrders", 24, 36);
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As Integer
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlTransaction. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "PublishOrders");
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String) As Integer
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteNonQuery(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns no resultset) against the specified SqlTransaction
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As Integer
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        Dim cmd As SqlCommand = New SqlCommand
        Dim mustCloseConnection As Boolean = False
        cmd.CommandTimeout = 0
        SqlHelper.PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, mustCloseConnection)
        ' Finally, execute the command
        Dim retval As Integer = cmd.ExecuteNonQuery
        ' Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()
        Return retval
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified 
    ''' SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  int result = ExecuteNonQuery(conn, trans, "PublishOrders", 24, 36);
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As Integer
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteDataset"

    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String) As DataSet
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteDataset(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the database specified in the connection string 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(connString, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As DataSet
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        Dim connection As SqlConnection = New SqlConnection(connectionString)
        connection.Open()
        ' Call the overload that takes a connection in place of the connection string
        Return SqlHelper.ExecuteDataset(connection, commandType, commandText, commandParameters)
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ''' the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(connString, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As DataSet
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders");
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String) As DataSet
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteDataset(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As DataSet
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        Dim cmd As SqlCommand = New SqlCommand
        ' cmd.CommandTimeout = 480;
        Dim mustCloseConnection As Boolean = False
        SqlHelper.PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, mustCloseConnection)
        ' Create the DataAdapter & DataSet
        Dim da As SqlDataAdapter = New SqlDataAdapter(cmd)
        Dim ds As DataSet = New DataSet
        ' da.SelectCommand.CommandTimeout = 480;
        ' Fill the DataSet using default values for DataTable names, etc
        da.Fill(ds)
        ' Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        If mustCloseConnection Then
            connection.Close()
        End If

        ' Return the dataset
        Return ds
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(conn, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As DataSet
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders");
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String) As DataSet
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteDataset(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As DataSet
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        Dim cmd As SqlCommand = New SqlCommand
        Dim mustCloseConnection As Boolean = False
        SqlHelper.PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, mustCloseConnection)
        ' Create the DataAdapter & DataSet
        Dim da As SqlDataAdapter = New SqlDataAdapter(cmd)
        Dim ds As DataSet = New DataSet
        ' Fill the DataSet using default values for DataTable names, etc
        da.Fill(ds)
        ' Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()
        ' Return the dataset
        Return ds
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified 
    ''' SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  DataSet ds = ExecuteDataset(trans, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As DataSet
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteReader"

    Private Enum SqlConnectionOwnership

        Internal

        External
    End Enum

    ''' <summary>
    ''' Create and prepare a SqlCommand, and call ExecuteReader with the appropriate CommandBehavior.
    ''' </summary>
    ''' <remarks>
    ''' If we created and opened the connection, we want the connection to be closed when the DataReader is closed.
    ''' 
    ''' If the caller provided the connection, we want to leave it to them to manage.
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection, on which to execute this command</param>
    ''' <param name="transaction">A valid SqlTransaction, or 'null'</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParameters to be associated with the command or 'null' if no parameters are required</param>
    ''' <param name="connectionOwnership">Indicates whether the connection parameter was provided by the caller, or created by SqlHelper</param>
    ''' <returns>SqlDataReader containing the results of the command</returns>
    Private Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ByVal commandParameters() As SqlParameter, ByVal connectionOwnership As SqlConnectionOwnership) As SqlDataReader
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        Dim mustCloseConnection As Boolean = False
        ' Create a command and prepare it for execution
        Dim cmd As SqlCommand = New SqlCommand
        Try
            SqlHelper.PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters, mustCloseConnection)
            ' Create a reader
            Dim dataReader As SqlDataReader
            ' Call ExecuteReader with the appropriate CommandBehavior
            If (connectionOwnership = SqlConnectionOwnership.External) Then
                dataReader = cmd.ExecuteReader
            Else
                dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection)
            End If

            ' Detach the SqlParameters from the command object, so they can be used again.
            ' HACK: There is a problem here, the output parameter values are fletched 
            ' when the reader is closed, so if the parameters are detached from the command
            ' then the SqlReader can�t set its values. 
            ' When this happen, the parameters can�t be used again in other command.
            Dim canClear As Boolean = True
            For Each commandParameter As SqlParameter In cmd.Parameters
                If (commandParameter.Direction <> ParameterDirection.Input) Then
                    canClear = False
                End If

            Next
            If canClear Then
                cmd.Parameters.Clear()
            End If

            Return dataReader
        Catch ex As System.Exception
            If mustCloseConnection Then
                connection.Close()
            End If

            Throw
        End Try

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the database specified in 
    ''' the connection string. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders");
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String) As SqlDataReader
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteReader(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the database specified in the connection string 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As SqlDataReader
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        Dim connection As SqlConnection = Nothing
        Try
            connection = New SqlConnection(connectionString)
            connection.Open()
            ' Call the private overload that takes an internally owned connection in place of the connection string
            Return SqlHelper.ExecuteReader(connection, Nothing, commandType, commandText, commandParameters, SqlConnectionOwnership.Internal)
        Catch ex As System.Exception
            ' If we fail to return the SqlDatReader, we need to close the connection ourselves
            If (Not (connection) Is Nothing) Then
                connection.Close()
            End If

            Throw
        End Try

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ''' the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(connString, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As SqlDataReader
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            Return SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders");
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String) As SqlDataReader
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteReader(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As SqlDataReader
        ' Pass through the call to the private overload using a null transaction value and an externally owned connection
        Return SqlHelper.ExecuteReader(connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, SqlConnectionOwnership.External)
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(conn, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As SqlDataReader
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            Return SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders");
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String) As SqlDataReader
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteReader(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''SqlDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As SqlDataReader
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        Return SqlHelper.ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, SqlConnectionOwnership.External)
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified
    ''' SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  SqlDataReader dr = ExecuteReader(trans, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As SqlDataReader
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            Return SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteScalar"

    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String) As Object
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteScalar(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a 1x1 resultset) against the database specified in the connection string 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As Object
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        Dim connection As SqlConnection = New SqlConnection(connectionString)
        connection.Open()
        ' Call the overload that takes a connection in place of the connection string
        Return SqlHelper.ExecuteScalar(connection, commandType, commandText, commandParameters)
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in 
    ''' the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(connString, "GetOrderCount", 24, 36);
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As Object
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlConnection. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount");
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String) As Object
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteScalar(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As Object
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        Dim cmd As SqlCommand = New SqlCommand
        Dim mustCloseConnection As Boolean = False
        SqlHelper.PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, mustCloseConnection)
        ' Execute the command & return the results
        Dim retval As Object = cmd.ExecuteScalar
        ' Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        If mustCloseConnection Then
            connection.Close()
        End If

        Return retval
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    ''' using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(conn, "GetOrderCount", 24, 36);
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As Object
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlTransaction. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount");
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String) As Object
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteScalar(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As Object
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        Dim cmd As SqlCommand = New SqlCommand
        Dim mustCloseConnection As Boolean = False
        SqlHelper.PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, mustCloseConnection)
        ' Execute the command & return the results
        Dim retval As Object = cmd.ExecuteScalar
        ' Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()
        Return retval
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified
    ''' SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  int orderCount = (int)ExecuteScalar(trans, "GetOrderCount", 24, 36);
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As Object
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' PPull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteXmlReader"

    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String) As XmlReader
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteXmlReader(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  XmlReader r = ExecuteXmlReader(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command using "FOR XML AUTO"</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An XmlReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As XmlReader
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        Dim mustCloseConnection As Boolean = False
        ' Create a command and prepare it for execution
        Dim cmd As SqlCommand = New SqlCommand
        Try
            SqlHelper.PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, mustCloseConnection)
            ' Create the DataAdapter & DataSet
            Dim retval As XmlReader = cmd.ExecuteXmlReader
            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()
            Return retval
        Catch ex As System.Exception
            If mustCloseConnection Then
                connection.Close()
            End If

            Throw
        End Try

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  XmlReader r = ExecuteXmlReader(conn, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure using "FOR XML AUTO"</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>An XmlReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As XmlReader
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  XmlReader r = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders");
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command using "FOR XML AUTO"</param>
    ''' <returns>An XmlReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String) As XmlReader
        ' Pass through the call providing null for the set of SqlParameters
        Return SqlHelper.ExecuteXmlReader(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  XmlReader r = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command using "FOR XML AUTO"</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <returns>An XmlReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter) As XmlReader
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        Dim cmd As SqlCommand = New SqlCommand
        Dim mustCloseConnection As Boolean = False
        SqlHelper.PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, mustCloseConnection)
        ' Create the DataAdapter & DataSet
        Dim retval As XmlReader = cmd.ExecuteXmlReader
        ' Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()
        Return retval
    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified 
    ''' SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  XmlReader r = ExecuteXmlReader(trans, "GetOrders", 24, 36);
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction, ByVal spName As String, ParamArray ByVal parameterValues() As Object) As XmlReader
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            Return SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            Return SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "FillDataset"

    Public Overloads Shared Sub FillDataset(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String)
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If (dataSet Is Nothing) Then
            Throw New ArgumentNullException("dataSet")
        End If

        Dim connection As SqlConnection = New SqlConnection(connectionString)
        connection.Open()
        ' Call the overload that takes a connection in place of the connection string
        SqlHelper.FillDataset(connection, commandType, commandText, dataSet, tableNames)
    End Sub

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the database specified in the connection string 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param>
    Public Overloads Shared Sub FillDataset(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String, ParamArray ByVal commandParameters() As SqlParameter)
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If (dataSet Is Nothing) Then
            Throw New ArgumentNullException("dataSet")
        End If

        Dim connection As SqlConnection = New SqlConnection(connectionString)
        connection.Open()
        ' Call the overload that takes a connection in place of the connection string
        SqlHelper.FillDataset(connection, commandType, commandText, dataSet, tableNames, commandParameters)
    End Sub

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ''' the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, 24);
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param> 
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    Public Overloads Shared Sub FillDataset(ByVal connectionString As String, ByVal spName As String, ByVal dataSet As DataSet, ByVal tableNames() As String, ParamArray ByVal parameterValues() As Object)
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If (dataSet Is Nothing) Then
            Throw New ArgumentNullException("dataSet")
        End If

        Dim connection As SqlConnection = New SqlConnection(connectionString)
        connection.Open()
        ' Call the overload that takes a connection in place of the connection string
        SqlHelper.FillDataset(connection, spName, dataSet, tableNames, parameterValues)
    End Sub

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  FillDataset(conn, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param> 
    Public Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String)
        SqlHelper.FillDataset(connection, commandType, commandText, dataSet, tableNames, Nothing)
    End Sub

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  FillDataset(conn, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    Public Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String, ParamArray ByVal commandParameters() As SqlParameter)
        SqlHelper.FillDataset(connection, Nothing, commandType, commandText, dataSet, tableNames, commandParameters)
    End Sub

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  FillDataset(conn, "GetOrders", ds, new string[] {"orders"}, 24, 36);
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    Public Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataSet As DataSet, ByVal tableNames() As String, ParamArray ByVal parameterValues() As Object)
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If (dataSet Is Nothing) Then
            Throw New ArgumentNullException("dataSet")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            SqlHelper.FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            SqlHelper.FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames)
        End If

    End Sub

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction. 
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  FillDataset(trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param>
    Public Overloads Shared Sub FillDataset(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String)
        SqlHelper.FillDataset(transaction, commandType, commandText, dataSet, tableNames, Nothing)
    End Sub

    ''' <summary>
    ''' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  FillDataset(trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    Public Overloads Shared Sub FillDataset(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String, ParamArray ByVal commandParameters() As SqlParameter)
        SqlHelper.FillDataset(transaction.Connection, transaction, commandType, commandText, dataSet, tableNames, commandParameters)
    End Sub

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified 
    ''' SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <remarks>
    ''' This method provides no access to output parameters or the stored procedure's return value parameter.
    ''' 
    ''' e.g.:  
    '''  FillDataset(trans, "GetOrders", ds, new string[]{"orders"}, 24, 36);
    ''' </remarks>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param>
    ''' <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
    Public Overloads Shared Sub FillDataset(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataSet As DataSet, ByVal tableNames() As String, ParamArray ByVal parameterValues() As Object)
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If (dataSet Is Nothing) Then
            Throw New ArgumentNullException("dataSet")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (parameterValues) Is Nothing) _
                    AndAlso (parameterValues.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Assign the provided values to these parameters based on parameter order
            SqlHelper.AssignParameterValues(commandParameters, parameterValues)
            ' Call the overload that takes an array of SqlParameters
            SqlHelper.FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters)
        Else
            ' Otherwise we can just call the SP without params
            SqlHelper.FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames)
        End If

    End Sub

    ''' <summary>
    ''' Private helper method that execute a SqlCommand (that returns a resultset) against the specified SqlTransaction and SqlConnection
    ''' using the provided parameters.
    ''' </summary>
    ''' <remarks>
    ''' e.g.:  
    '''  FillDataset(conn, trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection</param>
    ''' <param name="transaction">A valid SqlTransaction</param>
    ''' <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
    ''' <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
    ''' by a user defined name (probably the actual table name)
    ''' </param>
    ''' <param name="commandParameters">An array of SqlParamters used to execute the command</param>
    Private Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal transaction As SqlTransaction, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String, ParamArray ByVal commandParameters() As SqlParameter)
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If (dataSet Is Nothing) Then
            Throw New ArgumentNullException("dataSet")
        End If

        Dim command As SqlCommand = New SqlCommand
        Dim mustCloseConnection As Boolean = False
        SqlHelper.PrepareCommand(command, connection, transaction, commandType, commandText, commandParameters, mustCloseConnection)
        ' Create the DataAdapter & DataSet
        Dim dataAdapter As SqlDataAdapter = New SqlDataAdapter(command)
        ' Add the table mappings specified by the user
        If ((Not (tableNames) Is Nothing) _
                    AndAlso (tableNames.Length > 0)) Then
            Dim tableName As String = "Table"
            Dim index As Integer = 0
            Do While (index < tableNames.Length)
                If ((tableNames(index) Is Nothing) _
                            OrElse (tableNames(index).Length = 0)) Then
                    Throw New ArgumentException("The tableNames parameter must contain a list of tables, a value was provided as null or empty string." &
                        "", "tableNames")
                End If

                dataAdapter.TableMappings.Add(tableName, tableNames(index))
                tableName = (tableName + (index + 1).ToString)
                index = (index + 1)
            Loop

        End If

        ' Fill the DataSet using default values for DataTable names, etc
        dataAdapter.Fill(dataSet)
        ' Detach the SqlParameters from the command object, so they can be used again
        command.Parameters.Clear()

        If mustCloseConnection Then
            connection.Close()
        End If

    End Sub
#End Region
#Region "UpdateDataset"

    Public Shared Sub UpdateDataset(ByVal insertCommand As SqlCommand, ByVal deleteCommand As SqlCommand, ByVal updateCommand As SqlCommand, ByVal dataSet As DataSet, ByVal tableName As String)
        If (insertCommand Is Nothing) Then
            Throw New ArgumentNullException("insertCommand")
        End If

        If (deleteCommand Is Nothing) Then
            Throw New ArgumentNullException("deleteCommand")
        End If

        If (updateCommand Is Nothing) Then
            Throw New ArgumentNullException("updateCommand")
        End If

        If ((tableName Is Nothing) _
                    OrElse (tableName.Length = 0)) Then
            Throw New ArgumentNullException("tableName")
        End If

        Dim dataAdapter As SqlDataAdapter = New SqlDataAdapter
        ' Set the data adapter commands
        dataAdapter.UpdateCommand = updateCommand
        dataAdapter.InsertCommand = insertCommand
        dataAdapter.DeleteCommand = deleteCommand
        ' Update the dataset changes in the data source
        dataAdapter.Update(dataSet, tableName)
        ' Commit all the changes made to the DataSet
        dataSet.AcceptChanges()
    End Sub
#End Region
#Region "CreateCommand"

    Public Shared Function CreateCommand(ByVal connection As SqlConnection, ByVal spName As String, ParamArray ByVal sourceColumns() As String) As SqlCommand
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        Dim cmd As SqlCommand = New SqlCommand(spName, connection)
        cmd.CommandType = CommandType.StoredProcedure
        ' If we receive parameter values, we need to figure out where they go
        If ((Not (sourceColumns) Is Nothing) _
                    AndAlso (sourceColumns.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Assign the provided source columns to these parameters based on parameter order
            Dim index As Integer = 0
            Do While (index < sourceColumns.Length)
                commandParameters(index).SourceColumn = sourceColumns(index)
                index = (index + 1)
            Loop

            ' Attach the discovered parameters to the SqlCommand object
            SqlHelper.AttachParameters(cmd, commandParameters)
        End If

        Return cmd
    End Function
#End Region
#Region "ExecuteNonQueryTypedParams"

    Public Overloads Shared Function ExecuteNonQueryTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As Integer
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlConnection 
    ''' using the dataRow column values as the stored procedure's parameters values.  
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ''' </summary>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQueryTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As Integer
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified
    ''' SqlTransaction using the dataRow column values as the stored procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ''' </summary>
    ''' <param name="transaction">A valid SqlTransaction object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>An int representing the number of rows affected by the command</returns>
    Public Overloads Shared Function ExecuteNonQueryTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As Integer
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteDatasetTypedParams"

    Public Overloads Shared Function ExecuteDatasetTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As DataSet
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the dataRow column values as the store procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ''' </summary>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDatasetTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As DataSet
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ''' using the dataRow column values as the stored procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ''' </summary>
    ''' <param name="transaction">A valid SqlTransaction object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>A dataset containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteDatasetTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As DataSet
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteReaderTypedParams"

    Public Overloads Shared Function ExecuteReaderTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As SqlDataReader
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ''' using the dataRow column values as the stored procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReaderTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As SqlDataReader
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ''' using the dataRow column values as the stored procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <param name="transaction">A valid SqlTransaction object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>A SqlDataReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteReaderTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As SqlDataReader
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteScalarTypedParams"

    Public Overloads Shared Function ExecuteScalarTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As Object
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    ''' using the dataRow column values as the stored procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalarTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As Object
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
    ''' using the dataRow column values as the stored procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <param name="transaction">A valid SqlTransaction object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalarTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As Object
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
#Region "ExecuteXmlReaderTypedParams"

    Public Overloads Shared Function ExecuteXmlReaderTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As XmlReader
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName)
        End If

    End Function

    ''' <summary>
    ''' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ''' using the dataRow column values as the stored procedure's parameters values.
    ''' This method will query the database to discover the parameters for the 
    ''' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ''' </summary>
    ''' <param name="transaction">A valid SqlTransaction object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
    ''' <returns>An XmlReader containing the resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteXmlReaderTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As XmlReader
        If (transaction Is Nothing) Then
            Throw New ArgumentNullException("transaction")
        End If

        If ((Not (transaction) Is Nothing) _
                    AndAlso (transaction.Connection Is Nothing)) Then
            Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        If ((Not (dataRow) Is Nothing) _
                    AndAlso (dataRow.ItemArray.Length > 0)) Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)
            ' Set the parameters values
            SqlHelper.AssignParameterValues(commandParameters, dataRow)
            Return SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            Return SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName)
        End If

    End Function
#End Region
End Class

''' <summary>
''' SqlHelperParameterCache provides functions to leverage a static cache of procedure parameters, and the
''' ability to discover parameters for stored procedures at run-time.
''' </summary>
Public NotInheritable Class SqlHelperParameterCache
#Region "Private methods, variables, And constructors"

    Private Sub New()
        MyBase.New

    End Sub

    Private Shared paramCache As Hashtable = Hashtable.Synchronized(New Hashtable)

    ''' <summary>
    ''' Resolve at run time the appropriate set of SqlParameters for a stored procedure
    ''' </summary>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="includeReturnValueParameter">Whether or not to include their return value parameter</param>
    ''' <returns>The parameter array discovered.</returns>
    Private Shared Function DiscoverSpParameterSet(ByVal connection As SqlConnection, ByVal spName As String, ByVal includeReturnValueParameter As Boolean) As SqlParameter()
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        Dim cmd As SqlCommand = New SqlCommand(spName, connection)
        cmd.CommandType = CommandType.StoredProcedure
        connection.Open()
        SqlCommandBuilder.DeriveParameters(cmd)
        connection.Close()

        If Not includeReturnValueParameter Then
            cmd.Parameters.RemoveAt(0)
        End If

        Dim discoveredParameters() As SqlParameter = New SqlParameter((cmd.Parameters.Count) - 1) {}
        cmd.Parameters.CopyTo(discoveredParameters, 0)
        ' Init the parameters with a DBNull value
        For Each discoveredParameter As SqlParameter In discoveredParameters
            discoveredParameter.Value = DBNull.Value
        Next
        Return discoveredParameters
    End Function

    ''' <summary>
    ''' Deep copy of cached SqlParameter array
    ''' </summary>
    ''' <param name="originalParameters"></param>
    ''' <returns></returns>
    Private Shared Function CloneParameters(ByVal originalParameters() As SqlParameter) As SqlParameter()
        Dim clonedParameters() As SqlParameter = New SqlParameter((originalParameters.Length) - 1) {}
        Dim i As Integer = 0
        Dim j As Integer = originalParameters.Length
        Do While (i < j)
            clonedParameters(i) = CType(CType(originalParameters(i), ICloneable).Clone, SqlParameter)
            i = (i + 1)
        Loop

        Return clonedParameters
    End Function
#End Region
#Region "caching functions"

    Public Shared Sub CacheParameterSet(ByVal connectionString As String, ByVal commandText As String, ParamArray ByVal commandParameters() As SqlParameter)
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((commandText Is Nothing) _
                    OrElse (commandText.Length = 0)) Then
            Throw New ArgumentNullException("commandText")
        End If

        Dim hashKey As String = (connectionString + (":" + commandText))
        paramCache(hashKey) = commandParameters
    End Sub

    ''' <summary>
    ''' Retrieve a parameter array from the cache
    ''' </summary>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="commandText">The stored procedure name or T-SQL command</param>
    ''' <returns>An array of SqlParamters</returns>
    Public Shared Function GetCachedParameterSet(ByVal connectionString As String, ByVal commandText As String) As SqlParameter()
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((commandText Is Nothing) _
                    OrElse (commandText.Length = 0)) Then
            Throw New ArgumentNullException("commandText")
        End If

        Dim hashKey As String = (connectionString + (":" + commandText))
        Dim cachedParameters() As SqlParameter = CType(paramCache(hashKey), SqlParameter())
        If (cachedParameters Is Nothing) Then
            Return Nothing
        Else
            Return SqlHelperParameterCache.CloneParameters(cachedParameters)
        End If

    End Function
#End Region
#Region "Parameter Discovery Functions"

    Public Overloads Shared Function GetSpParameterSet(ByVal connectionString As String, ByVal spName As String) As SqlParameter()
        Return SqlHelperParameterCache.GetSpParameterSet(connectionString, spName, False)
    End Function

    ''' <summary>
    ''' Retrieves the set of SqlParameters appropriate for the stored procedure
    ''' </summary>
    ''' <remarks>
    ''' This method will query the database for this information, and then store it in a cache for future requests.
    ''' </remarks>
    ''' <param name="connectionString">A valid connection string for a SqlConnection</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="includeReturnValueParameter">A bool value indicating whether the return value parameter should be included in the results</param>
    ''' <returns>An array of SqlParameters</returns>
    Public Overloads Shared Function GetSpParameterSet(ByVal connectionString As String, ByVal spName As String, ByVal includeReturnValueParameter As Boolean) As SqlParameter()
        If ((connectionString Is Nothing) _
                    OrElse (connectionString.Length = 0)) Then
            Throw New ArgumentNullException("connectionString")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        Dim connection As SqlConnection = New SqlConnection(connectionString)
        Return SqlHelperParameterCache.GetSpParameterSetInternal(connection, spName, includeReturnValueParameter)
    End Function

    ''' <summary>
    ''' Retrieves the set of SqlParameters appropriate for the stored procedure
    ''' </summary>
    ''' <remarks>
    ''' This method will query the database for this information, and then store it in a cache for future requests.
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <returns>An array of SqlParameters</returns>
    Friend Overloads Shared Function GetSpParameterSet(ByVal connection As SqlConnection, ByVal spName As String) As SqlParameter()
        Return SqlHelperParameterCache.GetSpParameterSet(connection, spName, False)
    End Function

    ''' <summary>
    ''' Retrieves the set of SqlParameters appropriate for the stored procedure
    ''' </summary>
    ''' <remarks>
    ''' This method will query the database for this information, and then store it in a cache for future requests.
    ''' </remarks>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="includeReturnValueParameter">A bool value indicating whether the return value parameter should be included in the results</param>
    ''' <returns>An array of SqlParameters</returns>
    Friend Overloads Shared Function GetSpParameterSet(ByVal connection As SqlConnection, ByVal spName As String, ByVal includeReturnValueParameter As Boolean) As SqlParameter()
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        Dim clonedConnection As SqlConnection = CType(CType(connection, ICloneable).Clone, SqlConnection)
        Return SqlHelperParameterCache.GetSpParameterSetInternal(clonedConnection, spName, includeReturnValueParameter)
    End Function

    ''' <summary>
    ''' Retrieves the set of SqlParameters appropriate for the stored procedure
    ''' </summary>
    ''' <param name="connection">A valid SqlConnection object</param>
    ''' <param name="spName">The name of the stored procedure</param>
    ''' <param name="includeReturnValueParameter">A bool value indicating whether the return value parameter should be included in the results</param>
    ''' <returns>An array of SqlParameters</returns>
    Private Shared Function GetSpParameterSetInternal(ByVal connection As SqlConnection, ByVal spName As String, ByVal includeReturnValueParameter As Boolean) As SqlParameter()
        If (connection Is Nothing) Then
            Throw New ArgumentNullException("connection")
        End If

        If ((spName Is Nothing) _
                    OrElse (spName.Length = 0)) Then
            Throw New ArgumentNullException("spName")
        End If

        Dim hashKey As String = (connection.ConnectionString + ":" _
                    + spName + If(includeReturnValueParameter, ":include ReturnValue Parameter", ""))


        Dim cachedParameters() As SqlParameter
        cachedParameters = CType(paramCache(hashKey), SqlParameter())
        If (cachedParameters Is Nothing) Then
            Dim spParameters() As SqlParameter = SqlHelperParameterCache.DiscoverSpParameterSet(connection, spName, includeReturnValueParameter)
            paramCache(hashKey) = spParameters
            cachedParameters = spParameters
        End If

        Return SqlHelperParameterCache.CloneParameters(cachedParameters)
    End Function
#End Region
End Class
