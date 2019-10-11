using System;
using System.Data;



	/// <summary>
	/// Summary description for IDataAccess.
	/// </summary>
	public interface  IDataAccess
	{

		#region ExecuteNonQuery
		int ExecuteNonQuery(System.Data.IDbConnection connection,System.Data.CommandType commandType,string CommandText);
		int ExecuteNonQuery(string CommandText,System.Data.CommandType commandType);
		#endregion

		#region GetDBConnection
		IDbConnection GetDBConnection();
		#endregion

		#region ExecuteScalar
		object ExecuteScalar(System.Data.IDbConnection connection,System.Data.CommandType commandType,string CommandText);
		object ExecuteScalar(string CommandText,System.Data.CommandType commandType);
		#endregion
		
		#region ExecuteDataSet
		DataSet ExecuteDataSet(System.Data.IDbConnection connection, System.Data.CommandType commandType,string CommandText);
		DataSet ExecuteDataSet(string CommandText, System.Data.CommandType commandType);
		#endregion

		#region 支持事务处理的实现
		System.Data.IDbTransaction BeginTransaction();
		void Commit();
		void Rollback();
		#endregion

	}
