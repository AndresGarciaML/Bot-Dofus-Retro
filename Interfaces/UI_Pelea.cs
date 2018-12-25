﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Bot_Dofus_1._29._1.Otros;
using Bot_Dofus_1._29._1.Otros.Entidades.Personajes.Hechizos;
using Bot_Dofus_1._29._1.Otros.Mapas;
using Bot_Dofus_1._29._1.Otros.Mapas.Movimiento;
using Bot_Dofus_1._29._1.Otros.Peleas.Configuracion;
using Bot_Dofus_1._29._1.Otros.Peleas.Enums;

namespace Bot_Dofus_1._29._1.Interfaces
{
    public partial class UI_Pelea : UserControl
    {
        private Cuenta cuenta;

        public UI_Pelea(Cuenta _cuenta)
        {
            InitializeComponent();
            cuenta = _cuenta;

            refrescar_Lista_Hechizos();
            cuenta.personaje.hechizos_actualizados += actualizar_Agregar_Lista_Hechizos;
        }

        private void UI_Pelea_Load(object sender, EventArgs e)
        {
            comboBox_lista_posicionamiento.SelectedIndex = 2;
            comboBox_lista_tactica.SelectedIndex = 2;
            comboBox_focus_hechizo.SelectedIndex = 0;
        }

        private void actualizar_Agregar_Lista_Hechizos()
        {
            comboBox_lista_hechizos.Items.Clear();

            comboBox_lista_hechizos.DisplayMember = "nombre";
            comboBox_lista_hechizos.ValueMember = "id";
            comboBox_lista_hechizos.DataSource = cuenta.personaje.hechizos;

            comboBox_lista_hechizos.SelectedIndex = 0;
        }

        private void button_agregar_hechizo_Click(object sender, EventArgs e)
        {
            Hechizo hechizo = comboBox_lista_hechizos.SelectedItem as Hechizo;

            cuenta.pelea_extension.configuracion.hechizos.Add(new HechizoPelea(hechizo.id, hechizo.nombre, (HechizoFocus)comboBox_focus_hechizo.SelectedIndex, Convert.ToByte(numeric_lanzamientos_turno.Value)));
            cuenta.pelea_extension.configuracion.guardar();
            refrescar_Lista_Hechizos();
        }

        private void refrescar_Lista_Hechizos()
        {
            listView_hechizos_pelea.Items.Clear();

            cuenta.pelea_extension.configuracion.hechizos.ForEach(hechizo =>
            {
                listView_hechizos_pelea.Items.Add(hechizo.id.ToString()).SubItems.AddRange(new string[]
                {
                    hechizo.nombre, hechizo.focus.ToString(), hechizo.lanzamientos_x_turno.ToString()
                });
            });
        }

        private void button_subir_hechizo_Click(object sender, EventArgs e)
        {
            if (listView_hechizos_pelea.FocusedItem == null || listView_hechizos_pelea.FocusedItem.Index == 0)
                return;

            List<HechizoPelea> hechizo = cuenta.pelea_extension.configuracion.hechizos;
            HechizoPelea temporal = hechizo[listView_hechizos_pelea.FocusedItem.Index - 1];

            hechizo[listView_hechizos_pelea.FocusedItem.Index - 1] = hechizo[listView_hechizos_pelea.FocusedItem.Index];
            hechizo[listView_hechizos_pelea.FocusedItem.Index] = temporal;
            cuenta.pelea_extension.configuracion.guardar();
            refrescar_Lista_Hechizos();
        }

        private void button_bajar_hechizo_Click(object sender, EventArgs e)
        {
            if (listView_hechizos_pelea.FocusedItem == null || listView_hechizos_pelea.FocusedItem.Index == 0)
                return;

            List<HechizoPelea> hechizo = cuenta.pelea_extension.configuracion.hechizos;
            HechizoPelea temporal = hechizo[listView_hechizos_pelea.FocusedItem.Index + 1];

            hechizo[listView_hechizos_pelea.FocusedItem.Index + 1] = hechizo[listView_hechizos_pelea.FocusedItem.Index];
            hechizo[listView_hechizos_pelea.FocusedItem.Index] = temporal;
            cuenta.pelea_extension.configuracion.guardar();
            refrescar_Lista_Hechizos();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Mapa mapa = cuenta.personaje.mapa;
            if (cuenta.personaje.mapa.monstruos.Count > 0)
            {
                int celda_actual = cuenta.personaje.celda_id, celda_monstruo_destino = mapa.get_Monstruos().Values.ElementAt(0)[0].celda_id;

                if (celda_actual != celda_monstruo_destino & celda_monstruo_destino != -1)
                {
                    cuenta.logger.log_informacion("PELEAS", "Monstruo encontrado en la casilla " + celda_monstruo_destino);
                    switch (mapa.get_Mover_Celda_Resultado(celda_actual, celda_monstruo_destino, false))
                    {
                        case ResultadoMovimientos.EXITO:
                            cuenta.logger.log_informacion("PELEAS", "Desplazando para comenzar el combate");
                        break;

                        case ResultadoMovimientos.MISMA_CELDA:
                        case ResultadoMovimientos.FALLO:
                        case ResultadoMovimientos.PATHFINDING_ERROR:
                            cuenta.logger.log_Error("PELEAS", "El monstruo no esta en la casilla selecciona");
                        break;
                    }
                }
            }
        }

        private void checkbox_espectadores_CheckedChanged(object sender, EventArgs e)
        {
            cuenta.pelea_extension.configuracion.desactivar_espectador = checkbox_espectadores.Checked;
            cuenta.pelea_extension.configuracion.guardar();
        }
    }
}
