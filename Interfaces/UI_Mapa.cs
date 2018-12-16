﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Bot_Dofus_1._29._1.Controles.ControlMapa;
using Bot_Dofus_1._29._1.Otros;
using Bot_Dofus_1._29._1.Otros.Mapas;
using Bot_Dofus_1._29._1.Protocolo.Enums;
using Bot_Dofus_1._29._1.Utilidades.Extensiones;

namespace Bot_Dofus_1._29._1.Interfaces
{
    public partial class UI_Mapa : UserControl
    {
        private Cuenta cuenta = null;

        public UI_Mapa(Cuenta _cuenta)
        {
            InitializeComponent();
            cuenta = _cuenta;

            mapa.clic_celda += mapa_Control_Celda_Clic;
            cuenta.personaje.mapa_actualizado += eventos_Mapa_Cambiado;
        }

        private void eventos_Mapa_Cambiado()
        {
            Dictionary<int, Celda> celdas_mapa_personaje = cuenta.personaje.mapa.celdas;
            if (celdas_mapa_personaje != null && celdas_mapa_personaje.Count > 0)
            {
                for (int i = 0; i < celdas_mapa_personaje.Count; i++)
                {
                    switch (celdas_mapa_personaje[i].tipo_caminable)
                    {
                        case 0:
                            mapa.celdas[i].Celda_Estado = CeldaEstado.NO_CAMINABLE;
                        break;

                        case 6:
                        case 5:
                            mapa.celdas[i].Celda_Estado = CeldaEstado.CAMINO;
                        break;

                        case 1:
                            mapa.celdas[i].Celda_Estado = CeldaEstado.OBJETO_INTERACTIVO;
                        break;

                        case 2:
                            mapa.celdas[i].Celda_Estado = CeldaEstado.CELDA_TELEPORT;
                        break;

                        default:
                            mapa.celdas[i].Celda_Estado = CeldaEstado.CAMINABLE;
                        break;
                    }
                }
            }
            mapa.Invalidate();
        }

        private void mapa_Control_Celda_Clic(CeldaMapa celda, MouseButtons botones)
        {
            int celda_id_actual = cuenta.personaje.celda_id, celda_destino = celda.id;
            if (botones == MouseButtons.Left && celda_id_actual != 0 && celda_destino != 0)
            {
                if(cuenta.Estado_Cuenta == EstadoCuenta.CONECTADO_INACTIVO)
                {
                    cuenta.Estado_Cuenta = EstadoCuenta.MOVIMIENTO;
                    Pathfinding pathfinding = new Pathfinding(cuenta.personaje.mapa);
                    string camino = pathfinding.pathing(celda_id_actual, celda_destino);
                    if (!string.IsNullOrEmpty(camino))
                    {
                        cuenta.conexion.enviar_Paquete("GA001" + camino);
                        int distancia = pathfinding.get_Distancia_Estimada(celda_id_actual, celda_destino);
                        Extensiones.Delay(distancia * (distancia < 6 ? 300 : 250)).ContinueWith(x => 
                        {
                            cuenta.conexion.enviar_Paquete("GKK0");
                            cuenta.personaje.celda_id = celda_destino;
                            cuenta.Estado_Cuenta = EstadoCuenta.CONECTADO_INACTIVO;
                        });
                    }
                }
            }
        }
    }
}
