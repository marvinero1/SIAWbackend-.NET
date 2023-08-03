﻿using SIAW.Data;

namespace SIAW
{
    public class UserConnectionManager
    {
        private readonly Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        public void SetUserConnection(string userConn, string stringConection)
        {
            _userConnections[userConn] = stringConection;
        }

        public string GetUserConnection(string userConn)
        {
            if (_userConnections.TryGetValue(userConn, out var stringConection))
            {
                return stringConection;
            }
            return null; // Manejo de error si el contexto no se encuentra
        }
    }
}
