using System;
using System.Data;
using System.Collections;
using System.Data.OracleClient;
using System.Configuration;



namespace CopyFileServer
{
	/// <summary>
	/// Summary description for DB.
	/// </summary>
	/// 
	//实例化后将new 一个Connection对象,使用的是singlton的设计模式
	public class Oracle:IDataAccess
	{
		private  bool needTransaction = false;
		private  bool mustCloseConnection =  true;

		private  System.Data.IDbTransaction tran ;
		private string connectstring ;
          private string Errormsg_;
		public string ConnectString
		{
			get
			{
				return connectstring ;
			}
			set
			{
				connectstring = value;
			}
		}
        public string ErrorMsg
        {
            get
            {
                return Errormsg_;
            }
            set
            {
                Errormsg_ = value;
            }
        }
		private System.Data.OracleClient.OracleConnection   Connection ;


        public Oracle(string connectstring__,Boolean if_use )
		{
          
            if (if_use == true)
            {
                connectstring = connectstring__;
            }
            else
            {
                connectstring = System.Configuration.ConfigurationSettings.AppSettings["ConnectionString_Oracle"];
            }
			try
			{
                this.Connection = new OracleConnection(connectstring);
             
			}
            catch (OracleException ex)
			{
				throw ex;
			}
		}
        public string data_source
        {
            get
            {
                return Connection.DataSource;
            }
        }
        
       
        public Oracle()
        {
            try
            {
                string connstr = System.Configuration.ConfigurationSettings.AppSettings["ConnectionString_Oracle"];
             
                // connstr = connstr.Replace("Unicode=True", "Unicode=False");
                connectstring = connstr;
                this.Connection = new OracleConnection(connstr);
            }
            catch (OracleException ex)
            {
                throw ex;
            }
        }
        public Oracle(Boolean Unicode)
        {

            try
            {
                string connstr = System.Configuration.ConfigurationSettings.AppSettings["ConnectionString_Oracle"];
                connstr = connstr.Replace("Unicode=True", "Unicode=False");
                this.Connection = new OracleConnection(connstr);
            }
            catch (OracleException ex)
            {
                throw ex;
            }
        }
        public Oracle(string connectid)
        {

            try
            {
                string connstr = System.Configuration.ConfigurationSettings.AppSettings["ConnectionString_" + connectid ] ;
                connstr = connstr.Replace("Unicode=True", "Unicode=False");
                this.Connection = new OracleConnection(connstr);
             
            }
            catch (OracleException ex)
            {
                throw ex;
            }
        }

		public IDbConnection GetDBConnection()
		{
			return this.Connection;
		}

		#region PrepareCommand
		private  void PrepareCommand(IDbCommand command, IDbConnection connection, IDbTransaction transaction, 
			CommandType commandType, string commandText )
		{
			if( command == null ) throw new ArgumentNullException( "command" );
			if( commandText == null || commandText.Length == 0 ) throw new ArgumentNullException( "commandText" );

			// Associate the connection with the command
		
			command.Connection = connection;

			// Set the command text (stored procedure name or SQL statement)
			command.CommandText = commandText;
		
			// If we were provided a transaction, assign it
			if (transaction != null)
			{
				if( transaction.Connection == null ) 
					throw new ArgumentException( "The transaction was rollbacked or commited, please provide an open transaction.", "transaction" );
				command.Transaction = transaction;
			}

			// Set the command type
			command.CommandType = commandType;
			return;
		}

		
		#endregion

		#region ExecuteNonQuery
		

		

		public   int ExecuteNonQuery(System.Data.IDbConnection connection,System.Data.CommandType commandType,string commandText)
		{
			int affectRows;
            affectRows = -1;
            if (connection.State != System.Data.ConnectionState.Open)
				connection.Open();
            OracleCommand Command = new OracleCommand();
            
			if ( !this.needTransaction )
				tran = null;
            if (commandText.Trim().ToLower().IndexOf("declare ") == 0 || commandText.Trim().ToLower().IndexOf("begin ") == 0)
            {
                commandText = commandText +" ";
            }
            else
            {
                commandText = "begin " + commandText + "; end ;";
            }
			PrepareCommand(Command,connection,tran,CommandType.Text,commandText);
			try
			{
				affectRows = Command.ExecuteNonQuery();
			}
			catch( Exception ex)
			{
                affectRows = -1;
				needTransaction = false;
                Errormsg_ = ex.Message;
				throw ex;               
                return affectRows;
			}
            //finally
            //{
            //    if (mustCloseConnection)
            //    {
            //        if(!needTransaction)
            //        {
            //            connection.Close();
            //        }
            //    }

               
  
            //}
            return affectRows;
		}

		public int ExecuteNonQuery(string commandText, System.Data.CommandType commandType)
		{
			return this.ExecuteNonQuery(this.Connection,commandType,commandText);
		}

        public int ExecuteNonQuery(string commandText,System.Data.OracleClient.OracleParameter[] parameters)
        {
            int affectRows;
            affectRows = -1;
            if (this.Connection.State != System.Data.ConnectionState.Open)
                this.Connection.Open();
            OracleCommand Command = new OracleCommand();

            if (!this.needTransaction)
                tran = null;
            Command.CommandType = CommandType.StoredProcedure;
            PrepareCommand(Command, this.Connection, tran, CommandType.StoredProcedure, commandText);   
            for (int i = 0; i < parameters.Length; i++)
            {
                Command.Parameters.Add(parameters[i].ParameterName,parameters[i].OracleType,parameters[i].Size);
                Command.Parameters[i].Direction = parameters[i].Direction;
                Command.Parameters[i].Value = parameters[i].Value;
            }       
        
         
            try
            {
                affectRows = Command.ExecuteNonQuery();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Direction == ParameterDirection.Output || Command.Parameters[i].Direction == ParameterDirection.InputOutput)
                    {
                        parameters[i].Value = Command.Parameters[i].Value;
                    }
                }  
            }
            catch (Exception ex)
            {
                affectRows = -1;
                needTransaction = false;
                Errormsg_ = ex.Message;
                throw ex;
                return affectRows;
            }
            //finally
            //{
            //    if (mustCloseConnection)
            //    {
            //        if (!needTransaction)
            //        {
            //            this.Connection.Close();
            //        }
            //    }
            //}

            return affectRows;
            
        }
		#endregion

		#region ExecuteScalar
//		private object ExecuteIdentity(System.Data.IDbConnection connection, System.Data.CommandType commandType, string CommandText)
//		{
//			object identity;
////			string[] commands = CommandText.Split(';');(?<command>*)
//			string[] commands = SystemFramework.Utils.UtilMethods.SplitEx(CommandText,"$$$");//2005-10-19 用比较少用的"$$$"分割CommandText,防止用户输入带有 ; 的文本时导致Split出错
//
//			if(commands.Length < 1)
//				return null;
//
//			if (connection.State != System.Data.ConnectionState.Open)
//				connection.Open();
//
////			tran = connection.BeginTransaction();
//			SqlCommand  Command = (SqlCommand)connection.CreateCommand();
////			Command.Transaction = tran as SqlCeTransaction;
//			Command.CommandType = commandType;
//			Command.CommandText = commands[0];
//
//			try
//			{
//				identity = Command.ExecuteScalar();
//				if(commands.Length >= 2)
//				{
//					Command.CommandText = commands[1];
//					identity = Command.ExecuteScalar();
//				}
//
////				tran.Commit();
//				return identity;
//			}
//			catch(SqlException ex)
//			{
////				tran.Rollback();
//				needTransaction = false;
//				throw ex;
//			}
//			finally
//			{
//				if (mustCloseConnection)
//				{
//					if(!needTransaction)
//					{
//						connection.Close();
//					}
//				}
//			}
//		}

		public object ExecuteScalar(System.Data.IDbConnection connection,System.Data.CommandType commandType,string CommandText)
		{
			object affectRows;
			if (connection.State != System.Data.ConnectionState.Open)
				connection.Open();
            OracleCommand Command = new OracleCommand();
			if ( !this.needTransaction )
				tran = null;
			PrepareCommand(Command,connection,tran,CommandType.Text,CommandText);
			try
			{
				affectRows = Command.ExecuteScalar();
			}
			catch(OracleException ex)
			{
				needTransaction = false;
				throw ex;
			}
			finally
			{
				if (mustCloseConnection)
				{
					if(!needTransaction)
					{
						connection.Close();
					}
				}
			}
			return affectRows;
		}
		public object ExecuteScalar(string CommandText,System.Data.CommandType commandType)
		{
			return this.ExecuteScalar(this.Connection,commandType,CommandText);
//			return this.ExecuteIdentity(this.Connection, commandType, CommandText);
		}

		#endregion

		#region ExcuteReader
		public System.Data.OracleClient.OracleDataReader ExecuteReader(System.Data.IDbConnection connection, System.Data.CommandType commandType,string CommandText)
		{
			if (connection.State !=  System.Data.ConnectionState.Open)
				connection.Open();

			if (!this.needTransaction)
				tran = null;

            OracleCommand cmd = new OracleCommand();
			PrepareCommand( cmd , connection,tran,CommandType.Text,CommandText);
			OracleDataReader dr = null;

			try
			{
				return dr = (mustCloseConnection && !needTransaction)?cmd.ExecuteReader(CommandBehavior.CloseConnection):cmd.ExecuteReader();
			}
			catch(System.Data.OracleClient.OracleException ex)
			{
				this.Connection.Close();
				throw ex;
			}
		}

		public  OracleDataReader ExecuteReader(string commandText,System.Data.CommandType commandType)
		{
			return this.ExecuteReader(this.Connection,commandType,commandText);
		}
		#endregion

		#region ExecuteDataSet

		public DataSet ExecuteDataSet(System.Data.IDbConnection connection, System.Data.CommandType commandType,string CommandText,System.Data.DataSet ds,string tablename)
		{
			if (connection.State !=  System.Data.ConnectionState.Open)
				connection.Open();
			if (!this.needTransaction)
				tran = null;
            OracleCommand cmd = new OracleCommand();

			PrepareCommand( cmd , connection,tran,CommandType.Text,CommandText);

            OracleDataAdapter da = new OracleDataAdapter(cmd);
			

			try
			{
				da.Fill(ds,tablename);
				da.Dispose();
			}
            catch (System.Data.OracleClient.OracleException ex)
			{
				throw ex;
			}
			finally
			{
				if (mustCloseConnection)
				{
					if(!needTransaction)
					{
						Connection.Close();
					}
				}
			}
			return ds;

		}
		public DataSet ExecuteDataSet(System.Data.IDbConnection connection, System.Data.CommandType commandType,string CommandText)
		{
			if (connection.State !=  System.Data.ConnectionState.Open)
				connection.Open();
			if (!this.needTransaction)
				tran = null;
            OracleCommand cmd = new OracleCommand();

			PrepareCommand( cmd , connection,tran,CommandType.Text,CommandText);

            OracleDataAdapter da = new OracleDataAdapter(cmd);
			DataSet ds = new DataSet();

			try
			{
				da.Fill(ds);
				da.Dispose();
			}
            catch (System.Data.OracleClient.OracleException ex)
			{
				throw ex;
			}
			finally
			{
				if (mustCloseConnection)
				{
					if(!needTransaction)
					{
						Connection.Close();
					}
				}
			}
			return ds;

		}
		public DataSet ExecuteDataSet(string commandText,System.Data.CommandType commandType)
		{
			return this.ExecuteDataSet(this.Connection,commandType,commandText);
			
		}
		public DataSet ExecuteDataSet(string commandText,System.Data.CommandType commandType,System.Data.DataSet ds ,string tablename)
		{
			return this.ExecuteDataSet(this.Connection,commandType,commandText,ds,tablename);
			
		}

		public int ExcuteDataTable(DataTable srcTable, string commandText, System.Data.CommandType commandType)
		{
			if (this.Connection.State  !=  System.Data.ConnectionState.Open)
				this.Connection.Open();
			if (!this.needTransaction)
				tran = null;
            OracleCommand cmd = new OracleCommand();

			PrepareCommand( cmd , this.Connection,tran,CommandType.Text,commandText);

            OracleDataAdapter da = new OracleDataAdapter(cmd);

			try
			{
				da.Fill(srcTable);
				da.Dispose();
			}
            catch (System.Data.OracleClient.OracleException ex)
			{
                Errormsg_ = ex.Message;
                return -1;
			}
            //finally
            //{
            //    if (mustCloseConnection)
            //    {
            //        if(!needTransaction)
            //        {
            //            Connection.Close();
            //        }
            //    }
              
            //}
			return 1;
		}
		#endregion

		#region 事务处理

		public  System.Data.IDbTransaction  BeginTransaction()
		{
			this.needTransaction = true;
			if (this.Connection.State != System.Data.ConnectionState.Open)
				this.Connection.Open();
			this.tran = this.Connection.BeginTransaction();
			return this.tran;
		}

		public  void Commit()
		{
         
                if (this.tran != null)
                    this.tran.Commit();
                if (this.mustCloseConnection)
                    this.Connection.Close();
                this.needTransaction = false;
         
		}
		
		public  void Rollback()
		{
           
                if (this.tran != null)
                    this.tran.Rollback();
                if (this.mustCloseConnection)
                    this.Connection.Close();
                this.needTransaction = false;
            
		}

		#endregion	
	}
}

