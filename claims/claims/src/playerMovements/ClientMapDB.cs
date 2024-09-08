﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace claims.src.playerMovements
{
    public class ClientMapDB : SQLiteDBConnection
    {
        private SqliteCommand setMapPieceCmd;
        private SqliteCommand getMapPieceCmd;

        public ClientMapDB(ILogger logger) : base(logger)
        {
        }
        public override string DBTypeCode => "claims client saved plots";

        public override void OnOpened()
        {
            base.OnOpened();
            this.setMapPieceCmd = this.sqliteConn.CreateCommand();
            this.setMapPieceCmd.CommandText = "INSERT OR REPLACE INTO mappiece (position, data) VALUES (@pos, @data)";
            this.setMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
            this.setMapPieceCmd.Parameters.Add("@data", SqliteType.Blob);
            this.setMapPieceCmd.Prepare();
            this.getMapPieceCmd = this.sqliteConn.CreateCommand();
            this.getMapPieceCmd.CommandText = "SELECT data FROM mappiece WHERE position=@pos";
            this.getMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
            this.getMapPieceCmd.Prepare();
        }

        protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
        {
            using (SqliteCommand sqlite_cmd = sqliteConn.CreateCommand())
            {
                sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS mappiece (position integer PRIMARY KEY, data BLOB);";
                sqlite_cmd.ExecuteNonQuery();
            }
            /*using (SQLiteCommand sqlite_cmd2 = sqliteConn.CreateCommand())
            {
                sqlite_cmd2.CommandText = "CREATE TABLE IF NOT EXISTS blockidmapping (id integer PRIMARY KEY, data BLOB);";
                sqlite_cmd2.ExecuteNonQuery();
            }*/
        }
        public void Purge()
        {
            using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
            {
                cmd.CommandText = "delete FROM mappiece";
                cmd.ExecuteNonQuery();
            }
        }

        public ClientSavedZone[] GetMapPieces(List<Vec2i> zonesCoords)
        {
            ClientSavedZone[] pieces = new ClientSavedZone[zonesCoords.Count];
            for (int i = 0; i < zonesCoords.Count; i++)
            {
                this.getMapPieceCmd.Parameters["@pos"].Value = zonesCoords[i].ToChunkIndex();
                using (SqliteDataReader sqlite_datareader = this.getMapPieceCmd.ExecuteReader())
                {
                    while (sqlite_datareader.Read())
                    {
                        object data = sqlite_datareader["data"];
                        if (data == null)
                        {
                            return null;
                        }
                        pieces[i] = SerializerUtil.Deserialize<ClientSavedZone>(data as byte[]);
                    }
                }
            }
            return pieces;
        }
        public ClientSavedZone GetMapPiece(Vec2i zoneCoord)
        {
            this.getMapPieceCmd.Parameters["@pos"].Value = zoneCoord.ToChunkIndex();
            using (SqliteDataReader sqlite_datareader = this.getMapPieceCmd.ExecuteReader())
            {
                if (sqlite_datareader.Read())
                {
                    object data = sqlite_datareader["data"];
                    if (data == null)
                    {
                        return null;
                    }
                    return SerializerUtil.Deserialize<ClientSavedZone>(data as byte[]);
                }
            }
            return null;
        }

        public void SetMapPieces(Dictionary<Vec2i, ClientSavedZone> pieces)
        {
            using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
            {
                this.setMapPieceCmd.Transaction = transaction;
                foreach (KeyValuePair<Vec2i, ClientSavedZone> val in pieces)
                {
                    this.setMapPieceCmd.Parameters["@pos"].Value = val.Key.ToChunkIndex();
                    this.setMapPieceCmd.Parameters["@data"].Value = SerializerUtil.Serialize<ClientSavedZone>(val.Value);
                    this.setMapPieceCmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public override void Close()
        {
            SqliteCommand sqliteCommand = this.setMapPieceCmd;
            if (sqliteCommand != null)
            {
                sqliteCommand.Dispose();
            }
            SqliteCommand sqliteCommand2 = this.getMapPieceCmd;
            if (sqliteCommand2 != null)
            {
                sqliteCommand2.Dispose();
            }
            base.Close();
        }

        public override void Dispose()
        {
            SqliteCommand sqliteCommand = this.setMapPieceCmd;
            if (sqliteCommand != null)
            {
                sqliteCommand.Dispose();
            }
            SqliteCommand sqliteCommand2 = this.getMapPieceCmd;
            if (sqliteCommand2 != null)
            {
                sqliteCommand2.Dispose();
            }
            base.Dispose();
        }


    }
}
