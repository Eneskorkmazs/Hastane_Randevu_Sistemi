#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.Sqlite, 8.0.0"

using Microsoft.Data.Sqlite;

var dbPath = @"C:\Users\Enes\Desktop\Hastane_Randevu_Sistemi-main\HastaneRandevuSistemi (2)\HastaneRandevuSistemi\HastaneRandevuSistemi\HastaneRandevuSistemi.local.dev.db";

var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

// admin@hastane.com (ID: 7547f475...) kullanicisini Admin rolunden cikar
// Once hangi role kaydina sahip oldugunu bulalim
var cmdCheck = conn.CreateCommand();
cmdCheck.CommandText = @"
    SELECT ur.UserId, ur.RoleId, r.Name 
    FROM AspNetUserRoles ur 
    JOIN AspNetRoles r ON r.Id = ur.RoleId
    WHERE ur.UserId = '7547f475-8a0b-430c-bb79-1ef4cb2c8390'";
var rCheck = cmdCheck.ExecuteReader();
Console.WriteLine("admin@hastane.com rol kayitlari:");
while (rCheck.Read()) {
    Console.WriteLine($"  UserId:{rCheck["UserId"]} RoleId:{rCheck["RoleId"]} Role:{rCheck["Name"]}");
}
rCheck.Close();

// Admin rolunden cikar
var cmdDel = conn.CreateCommand();
cmdDel.CommandText = @"
    DELETE FROM AspNetUserRoles 
    WHERE UserId = '7547f475-8a0b-430c-bb79-1ef4cb2c8390'
    AND RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')";
int affected = cmdDel.ExecuteNonQuery();
Console.WriteLine($"\nadmin@hastane.com Admin rolunden cikarildi. Etkilenen satir: {affected}");

// Dogrula
var cmdVerify = conn.CreateCommand();
cmdVerify.CommandText = @"SELECT u.Email FROM AspNetUsers u JOIN AspNetUserRoles ur ON u.Id = ur.UserId JOIN AspNetRoles r ON r.Id = ur.RoleId WHERE r.Name = 'Admin'";
var rV = cmdVerify.ExecuteReader();
Console.WriteLine("\n=== Guncellenmis Admin Listesi ===");
while (rV.Read()) Console.WriteLine($"  {rV["Email"]}");
rV.Close();

conn.Close();
Console.WriteLine("\nIslem tamamlandi.");
