﻿using System;
using Bot_Dofus_1._29._1.LibreriaSockets;
using Bot_Dofus_1._29._1.Otros.Mapas;
using Bot_Dofus_1._29._1.Otros.Personajes;
using Bot_Dofus_1._29._1.Protocolo.Enums;
using Bot_Dofus_1._29._1.Protocolo.Game;
using Bot_Dofus_1._29._1.Protocolo.Login;
using Bot_Dofus_1._29._1.Utilidades.Logs;

namespace Bot_Dofus_1._29._1.Otros
{
    public class Cuenta : IDisposable
    {
        public string usuario { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string apodo { get; set; } = string.Empty;
        public string key_bienvenida { get; set; } = string.Empty;
        public string tiquet_game { get; set; } = string.Empty;
        public int servidor_id { get; set; } = 0;
        public Logger logger { get; private set; }
        public ClienteProtocolo conexion = null;
        public Personaje personaje = null;
        private EstadoCuenta estado_cuenta;
        private EstadoSocket fase_socket = EstadoSocket.NINGUNO;

        public event Action evento_estado_cuenta;
        public event Action<ClienteProtocolo> evento_fase_socket;

        public Cuenta(string _usuario, string _password, int _servidor_id)
        {
            usuario = _usuario;
            password = _password;
            servidor_id = _servidor_id;
            logger = new Logger();
            conexion = new Login("34.251.172.139", 443, this);
        }

        public string get_Nombre_Servidor() => servidor_id == 601 ? "Eratz" : "Henual";

        public void cambiando_Al_Servidor_Juego(string ip, int puerto)
        {
            if (fase_socket != EstadoSocket.NINGUNO)
            {
                conexion = new Game(ip, puerto, this);
                Fase_Socket = EstadoSocket.CAMBIANDO_A_JUEGO;
            }
        }

        public EstadoCuenta Estado_Cuenta
        {
            get => estado_cuenta;
            set
            {
                estado_cuenta = value;
                evento_estado_cuenta?.Invoke();
            }
        }

        public EstadoSocket Fase_Socket
        {
            get => fase_socket;
            internal set
            {
                EstadoSocket antiguo_valor = fase_socket;
                fase_socket = value;

                if (antiguo_valor != fase_socket)
                {
                    evento_fase_socket?.Invoke(conexion);
                }
            }
        }

        ~Cuenta()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            try
            {
                usuario = null;
                password = null;
                key_bienvenida = null;
                conexion = null;
                logger = null;
                personaje.Dispose(true);
                personaje = null;
                apodo = null;
                Estado_Cuenta = EstadoCuenta.DESCONECTADO;
                Fase_Socket = EstadoSocket.NINGUNO;
            }
            catch { }
        }
    }
}
