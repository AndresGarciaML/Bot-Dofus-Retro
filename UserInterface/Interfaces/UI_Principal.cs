﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Bot_Dofus_1._29._1.Game.Enums;
using Bot_Dofus_1._29._1.Interfaces;
using Bot_Dofus_1._29._1.Managers.Accounts;
using Bot_Dofus_1._29._1.Managers.Characters;
using Bot_Dofus_1._29._1.UserInterface.Forms;
using Bot_Dofus_1._29._1.Utilities.Extensions;
using Bot_Dofus_1._29._1.Utilities.Logs;

/*
    Este archivo es parte del proyecto BotDofus_1.29.1

    BotDofus_1.29.1 Copyright (C) 2019 Alvaro Prendes — Todos los derechos reservados.
    Creado por Alvaro Prendes
    web: http://www.salesprendes.com
*/

namespace Bot_Dofus_1._29._1.UserInterface.Interfaces
{
    public partial class UI_Principal : UserControl
    {
        private Account cuenta;
        private string nombre_cuenta;

        public UI_Principal(Account _cuenta)
        {
            InitializeComponent();
            cuenta = _cuenta;
            nombre_cuenta = cuenta.Configuration.Username; ;
        }

        private void UI_Principal_Load(object sender, EventArgs e)
        {
            desconectarOconectarToolStripMenuItem.Text = "Connecté";
            escribir_mensaje($"[{DateTime.Now.ToString("HH:mm:ss")}] -> [INFORMATION] Bot crée par Alvaro revue par Dyshay, http://www.salesprendes.com version: 1.0.0", LogTypes.ERROR.ToString("X"));

            cuenta.accountStateEvent += eventos_Estados_Cuenta;
            cuenta.accountDisconnectEvent += desconectar_Cuenta;
            cuenta.logger.log_event += (mensaje, color) => escribir_mensaje(mensaje.ToString(), color);

            cuenta.script.evento_script_cargado += evento_Scripts_Cargado;
            cuenta.script.evento_script_iniciado += evento_Scripts_Iniciado;
            cuenta.script.evento_script_detenido += evento_Scripts_Detenido;

            cuenta.Game.Character.caracteristicas_actualizadas += caracteristicas_Actualizadas;
            cuenta.Game.Character.pods_actualizados += pods_Actualizados;
            cuenta.Game.Character.servidor_seleccionado += servidor_Seleccionado;
            cuenta.Game.Character.personaje_seleccionado += personaje_Seleccionado;

            if (cuenta.hasGroup)
                escribir_mensaje("[" + DateTime.Now.ToString("HH:mm:ss") + "] -> Le chef de groupe est: " + cuenta.group.lider.Configuration.Username, LogTypes.ERROR.ToString("X"));
        }

        private void eliminarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Principal.LoadedAccounts.ContainsKey(nombre_cuenta))
            {
                if (cuenta.hasGroup && cuenta.isGroupLeader)
                    cuenta.group.desconectar_Cuentas();
                else if (cuenta.hasGroup)
                    cuenta.group.eliminar_Miembro(cuenta);

                cuenta.Dispose();
                Principal.LoadedAccounts[nombre_cuenta].contenido.Dispose();
                Principal.LoadedAccounts.Remove(nombre_cuenta);
            }
        }

        private void cambiar_Tab_Imagen(Image image)
        {
            if (Principal.LoadedAccounts.ContainsKey(nombre_cuenta))
                Principal.LoadedAccounts[nombre_cuenta].cabezera.propiedad_Imagen = image;
        }

        private void button_limpiar_consola_Click(object sender, EventArgs e) => textbox_logs.Clear();

        private void desconectarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (desconectarOconectarToolStripMenuItem.Text.Equals("Connecté"))
            {
                while (tabControl_principal.TabPages.Count > 2)
                    tabControl_principal.TabPages.RemoveAt(2);

                cuenta.Connect();

                cuenta.connexion.packetReceivedEvent += debugger.paquete_Recibido;
                cuenta.connexion.packetSendEvent += debugger.paquete_Enviado;
                cuenta.connexion.socketInformationEvent += get_Mensajes_Socket_Informacion;

                desconectarOconectarToolStripMenuItem.Text = "Deconnecté";
            }
            else if (desconectarOconectarToolStripMenuItem.Text.Equals("Deconnecté"))
                cuenta.Disconnect();
        }

        private void desconectar_Cuenta()
        {
            if (!IsHandleCreated)
                return;

            BeginInvoke((Action)(() =>
            {
                cambiar_Todos_Controles_Chat(false);

                for (int i = 2; i < tabControl_principal.TabPages.Count; ++i)
                    tabControl_principal.TabPages[i].Enabled = false;

                desconectarOconectarToolStripMenuItem.Text = "Connecté";
            }));
        }

        private void cambiar_Todos_Controles_Chat(bool estado)
        {
            BeginInvoke((Action)(() =>
            {
                canal_informaciones.Enabled = estado;
                canal_general.Enabled = estado;
                canal_privado.Enabled = estado;
                canal_gremio.Enabled = estado;
                canal_alineamiento.Enabled = estado;
                canal_reclutamiento.Enabled = estado;
                canal_comercio.Enabled = estado;
                canal_incarnam.Enabled = estado;
                comboBox_lista_canales.Enabled = estado;
                textBox_enviar_consola.Enabled = estado;
                cargarScriptToolStripMenuItem.Enabled = estado;
            }));
        }

        private void eventos_Estados_Cuenta()
        {
            switch (cuenta.accountState)
            {
                case AccountState.DISCONNECTED:
                    cambiar_Tab_Imagen(Properties.Resources.circulo_rojo);
                    break;

                case AccountState.CONNECTED:
                    cambiar_Tab_Imagen(Properties.Resources.circulo_naranja);
                    break;

                default:
                    cambiar_Tab_Imagen(Properties.Resources.circulo_verde);
                    break;
            }

            if (cuenta != null && Principal.LoadedAccounts.ContainsKey(nombre_cuenta))
                Principal.LoadedAccounts[nombre_cuenta].cabezera.propiedad_Estado = cuenta.accountState.cadena_Amigable();
        }

        private void agregar_Tab_Pagina(string nombre, UserControl control, int imagen_index)
        {
            tabControl_principal.BeginInvoke((Action)(() =>
            {
                control.Dock = DockStyle.Fill;
                TabPage nueva_pagina = new TabPage(nombre)
                {
                    ImageIndex = imagen_index
                };
                nueva_pagina.Controls.Add(control);
                tabControl_principal.TabPages.Add(nueva_pagina);
            }));
        }

        private void cargar_Canales_Chat()
        {
            BeginInvoke((Action)(() =>
            {
                canal_informaciones.Checked = cuenta.Game.Character.canales.Contains("i");
                canal_general.Checked = cuenta.Game.Character.canales.Contains("*");
                canal_privado.Checked = cuenta.Game.Character.canales.Contains("#");
                canal_gremio.Checked = cuenta.Game.Character.canales.Contains("%");
                canal_alineamiento.Checked = cuenta.Game.Character.canales.Contains("!");
                canal_reclutamiento.Checked = cuenta.Game.Character.canales.Contains("?");
                canal_comercio.Checked = cuenta.Game.Character.canales.Contains(":");
                canal_incarnam.Checked = cuenta.Game.Character.canales.Contains("^");
                comboBox_lista_canales.SelectedIndex = 0;
            }));
        }

        private void canal_Chat_Click(object sender, EventArgs e)
        {
            if (cuenta?.accountState != AccountState.DISCONNECTED && cuenta?.accountState != AccountState.CONNECTED)
            {
                string[] canales = { "i", "*", "#$p", "%", "!", "?", ":", "^" };
                CheckBox control = sender as CheckBox;

                cuenta.connexion.SendPacket((control.Checked ? "cC+" : "cC-") + canales[control.TabIndex]);
            }
        }

        private void textBox_enviar_consola_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBox_enviar_consola.TextLength > 0 && textBox_enviar_consola.TextLength < 255)
            {
                switch (textBox_enviar_consola.Text.ToUpper())
                {
                    case "/MAPID":
                        escribir_mensaje(cuenta.Game.Map.mapId.ToString(), "0040FF");
                    break;

                    case "/CELLID":
                        escribir_mensaje(cuenta.Game.Character.Cell.cellId.ToString(), "0040FF");
                    break;

                    case "/PING":
                        if (cuenta.connexion != null)
                            cuenta.connexion.SendPacket("ping", true);
                        else
                            escribir_mensaje("No estas conectado a dofus", "0040FF");
                    break;

                    default:
                        switch (comboBox_lista_canales.SelectedIndex)
                        {
                            case 0://General
                                cuenta.connexion.SendPacket("BM*|" + textBox_enviar_consola.Text + "|", true);
                                break;

                            case 1://Reclutamiento
                                cuenta.connexion.SendPacket("BM?|" + textBox_enviar_consola.Text + "|", true);
                                break;

                            case 2://Comercio
                                cuenta.connexion.SendPacket("BM:|" + textBox_enviar_consola.Text + "|", true);
                                break;

                            case 3://Mensaje privado
                                cuenta.connexion.SendPacket("BM" + textBox_nombre_privado.Text + "|" + textBox_enviar_consola.Text + "|", true);
                                break;
                        }
                    break;
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
                textBox_nombre_privado.Clear();
                textBox_enviar_consola.Clear();
            }
        }

        private void comboBox_lista_canales_Valor_Cambiado(object sender, EventArgs e)
        {
            ComboBox control = sender as ComboBox;
            textBox_nombre_privado.Enabled = control.SelectedIndex == 3;
        }

        private void cargarScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Sélectionnez le script pour le bot";
                    ofd.Filter = "Extension (.lua) | *.lua";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        cuenta.script.get_Desde_Archivo(ofd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                cuenta.logger.log_Error("SCRIPT", ex.Message);
            }
        }

        #region Actualizaciones Personaje
        private void caracteristicas_Actualizadas()
        {
            BeginInvoke((Action)(() =>
            {
                Character personaje = cuenta.Game.Character;

                progresBar_vitalidad.valor_Maximo = personaje.caracteristicas.vitalidad_maxima;
                progresBar_vitalidad.Valor = personaje.caracteristicas.vitalidad_actual;
                progresBar_energia.valor_Maximo = personaje.caracteristicas.maxima_energia;
                progresBar_energia.Valor = personaje.caracteristicas.energia_actual;
                progresBar_experiencia.Text = personaje.nivel.ToString();
                progresBar_experiencia.Valor = personaje.porcentaje_experiencia;
                label_kamas_principal.Text = personaje.kamas.ToString("0,0");
            }));
        }

        private void pods_Actualizados()
        {
            BeginInvoke((Action)(() =>
            {
                progresBar_pods.valor_Maximo = cuenta.Game.Character.inventario.pods_maximos;
                progresBar_pods.Valor = cuenta.Game.Character.inventario.pods_actuales;
            }));
        }

        private void servidor_Seleccionado()
        {
            agregar_Tab_Pagina("Personnage", new UI_Personaje(cuenta), 2);
            agregar_Tab_Pagina("Inventaire", new UI_Inventario(cuenta), 3);
        }

        private void personaje_Seleccionado()
        {
            cuenta.fightExtension.configuracion.cargar();
            agregar_Tab_Pagina("Map", new UI_Mapa(cuenta), 4);
            agregar_Tab_Pagina("Combat", new UI_Pelea(cuenta), 5);

            cambiar_Todos_Controles_Chat(true);
            cargar_Canales_Chat();
        }
        #endregion

        #region Mensajes
        private void get_Mensajes_Socket_Informacion(object error) => escribir_mensaje("[" + DateTime.Now.ToString("HH:mm:ss") + "] [Connexion] " + error, LogTypes.WARNING.ToString("X"));

        private void escribir_mensaje(string mensaje, string color)
        {
            if (!IsHandleCreated)
                return;

            textbox_logs.BeginInvoke((Action)(() =>
            {
                textbox_logs.Select(textbox_logs.TextLength, 0);
                textbox_logs.SelectionColor = ColorTranslator.FromHtml("#" + color);
                textbox_logs.AppendText(mensaje + Environment.NewLine);
                textbox_logs.ScrollToCaret();
            }));
        }
        #endregion

        #region Scripts
        private void iniciarScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!cuenta.script.activado)
                cuenta.script.activar_Script();
            else
                cuenta.script.detener_Script();
        }

        private void evento_Scripts_Cargado(string nombre)
        {
            cuenta.logger.log_informacion("SCRIPT", $"'{nombre}' chargée.");
            BeginInvoke((Action)(() =>
            {
                ScriptTituloStripMenuItem.Text = $"{(nombre.Length > 16 ? nombre.Substring(0, 16) : nombre)}";
                iniciarScriptToolStripMenuItem.Enabled = true;
            }));
        }

        private void evento_Scripts_Iniciado()
        {
            cuenta.logger.log_informacion("SCRIPT", "Initié");
            BeginInvoke((Action)(() =>
            {
                cargarScriptToolStripMenuItem.Enabled = false;
                iniciarScriptToolStripMenuItem.Image = Properties.Resources.boton_stop;
            }));
        }

        private void evento_Scripts_Detenido(string motivo)
        {
            if (string.IsNullOrEmpty(motivo))
                cuenta.logger.log_informacion("SCRIPT", "Arrêté");
            else
                cuenta.logger.log_informacion("SCRIPT", $"Arrêté à cause de {motivo}");

            BeginInvoke((Action)(() =>
            {
                iniciarScriptToolStripMenuItem.Image = Properties.Resources.boton_play;
                cargarScriptToolStripMenuItem.Enabled = true;
                ScriptTituloStripMenuItem.Text = "-";
            }));
        }
        #endregion
    }
}
