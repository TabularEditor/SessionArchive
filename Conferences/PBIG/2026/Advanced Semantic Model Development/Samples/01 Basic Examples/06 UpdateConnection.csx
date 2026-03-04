var dataSource = Model.DataSources["SpacePartsCoDW"] as ProviderDataSource;
var connectionString = Environment.GetEnvironmentVariable("SPACEPARTS_CONNECTIONSTRING_LOCALE");
if(string.IsNullOrEmpty(connectionString))
{
    Error("Environment variable SPACEPARTS_CONNECTIONSTRING_LOCALE not set!");
    //return;
}
connectionString = "data source=te3-training-eu.database.windows.net;initial catalog=SpacePartsCoDW;persist security info=True;user id=dwreader;Password=TE3#reader!";
dataSource.ConnectionString = connectionString;
dataSource.Provider = "System.Data.SqlClient";