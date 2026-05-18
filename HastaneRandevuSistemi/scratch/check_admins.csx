#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.Sqlite, 8.0.0"

using Microsoft.Data.Sqlite;

var dbPath = @"C:\Users\Enes\Desktop\Hastane_Randevu_Sistemi-main\HastaneRandevuSistemi (2)\HastaneRandevuSistemi\HastaneRandevuSistemi\HastaneRandevuSistemi.local.dev.db";
Console.WriteLine($"DB: {dbPath}");
Console.WriteLine($"Exists: {File.Exists(dbPath)}");

var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

Console.WriteLine("\n=== Admin Rolündeki Kullanıcılar ===");
var cmd = conn.CreateCommand();
cmd.CommandText = @"SELECT u.Email, u.Id FROM AspNetUsers u JOIN AspNetUserRoles ur ON u.Id = ur.UserId JOIN AspNetRoles r ON r.Id = ur.RoleId WHERE r.Name = 'Admin'";
var reader = cmd.ExecuteReader();
int count = 0;
while (reader.Read()) {
    count++;
    Console.WriteLine($"  {count}. Email: {reader["Email"]}  ID: {reader["Id"]}");
}
reader.Close();
Console.WriteLine($"Toplam admin: {count}");

Console.WriteLine("\n=== Son 10 Destek Talebi Bildirimi ===");
var cmd2 = conn.CreateCommand();
cmd2.CommandText = @"SELECT n.Id, n.UserId, n.Title, n.CreatedDate FROM Notifications n WHERE n.Type='Destek' ORDER BY n.CreatedDate DESC LIMIT 10";
var r2 = cmd2.ExecuteReader();
while (r2.Read()) {
    Console.WriteLine($"  NotifID:{r2["Id"]} -> UserId:{r2["UserId"]} | {r2["Title"]} | {r2["CreatedDate"]}");
}
r2.Close();
conn.Close();
