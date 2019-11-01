﻿using Bot_Dofus_1._29._1.Otros.Enums;
using Bot_Dofus_1._29._1.Otros.Game.Entidades.Manejadores.Recolecciones;
using Bot_Dofus_1._29._1.Otros.Game.Character;
using Bot_Dofus_1._29._1.Otros.Mapas.Entidades;
using Bot_Dofus_1._29._1.Otros.Scripts.Acciones;
using Bot_Dofus_1._29._1.Otros.Scripts.Acciones.Mapas;
using Bot_Dofus_1._29._1.Otros.Scripts.Acciones.Npcs;
using Bot_Dofus_1._29._1.Utilities;
using MoonSharp.Interpreter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot_Dofus_1._29._1.Otros.Scripts.Manejadores
{
    public class ActionsManager : IDisposable
    {
        private Account cuenta;
        public LuaScriptManager manejador_script;
        private ConcurrentQueue<ScriptAction> fila_acciones;
        public ScriptAction accion_actual;
        private DynValue coroutine_actual;
        private TimerWrapper timer_out;
        public int contador_pelea, contador_recoleccion, contador_peleas_mapa;
        private bool mapa_cambiado;
        private bool disposed;

        public event Action<bool> evento_accion_normal;
        public event Action<bool> evento_accion_personalizada;

        public ActionsManager(Account _cuenta, LuaScriptManager _manejador_script)
        {
            cuenta = _cuenta;
            manejador_script = _manejador_script;
            fila_acciones = new ConcurrentQueue<ScriptAction>();
            timer_out = new TimerWrapper(60000, time_Out_Callback);
            CharacterClass personaje = cuenta.game.character;
            
            cuenta.game.map.mapRefreshEvent += evento_Mapa_Cambiado;
            cuenta.game.fight.pelea_creada += get_Pelea_Creada;
            cuenta.game.manager.movimientos.movimiento_finalizado += evento_Movimiento_Celda;
            personaje.dialogo_npc_recibido += npcs_Dialogo_Recibido;
            personaje.dialogo_npc_acabado += npcs_Dialogo_Acabado;
            personaje.inventario.almacenamiento_abierto += iniciar_Almacenamiento;
            personaje.inventario.almacenamiento_cerrado += cerrar_Almacenamiento;
            cuenta.game.manager.recoleccion.recoleccion_iniciada += get_Recoleccion_Iniciada;
            cuenta.game.manager.recoleccion.recoleccion_acabada += get_Recoleccion_Acabada;
        }

        private void evento_Mapa_Cambiado()
        {
            if (!cuenta.script.InExecution || accion_actual == null)
                return;

            mapa_cambiado = true;

            // cuando inicia una pelea "resetea el mapa"
            if (!(accion_actual is PeleasAccion))
                contador_peleas_mapa = 0;

            if (accion_actual is ChangeMapAction || accion_actual is PeleasAccion || accion_actual is RecoleccionAccion || coroutine_actual != null)
            {
                limpiar_Acciones();
                acciones_Salida(1500);
            }
        }

        private async void evento_Movimiento_Celda(bool es_correcto)
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is PeleasAccion)
            {
                if (es_correcto)
                {
                    for (int delay = 0; delay < 10000 && cuenta.accountState != AccountStates.FIGHTING; delay += 500)
                        await Task.Delay(500);

                    if (cuenta.accountState != AccountStates.FIGHTING)
                    {
                        cuenta.logger.log_Peligro("SCRIPT", "Erreur en lançant le combat, les monstres ont pu se déplacer ou être volés !");
                        acciones_Salida(0);
                    }
                }
            }
            else if (accion_actual is MoverCeldaAccion celda)
            {
                if (es_correcto)
                    acciones_Salida(0);
                else
                    cuenta.script.detener_Script("erreur lors du déplacement vers la cellule" + celda.celda_id);
            }
            else if (accion_actual is ChangeMapAction && !es_correcto)
                cuenta.script.detener_Script("erreur lors du changement de carte");
        }

        private void get_Recoleccion_Iniciada()
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is RecoleccionAccion)
            {
                contador_recoleccion++;

                if (manejador_script.get_Global_Or("COMPTEUR_RECOLTE", DataType.Boolean, false))
                    cuenta.logger.log_informacion("SCRIPT", $"RECOLTE #{contador_recoleccion}");
            }
        }

        private void get_Recoleccion_Acabada(RecoleccionResultado resultado)
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is RecoleccionAccion)
            {
                switch (resultado)
                {
                    case RecoleccionResultado.FALLO:
                        cuenta.script.detener_Script("Erreur de récolte");
                    break;

                    default:
                        acciones_Salida(800);
                    break;
                }
            }
        }

        private void get_Pelea_Creada()
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is PeleasAccion)
            {
                timer_out.Stop();
                contador_peleas_mapa++;
                contador_pelea++;

                if (manejador_script.get_Global_Or("COMPTEUR_COMBAT", DataType.Boolean, false))
                    cuenta.logger.log_informacion("SCRIPT", $"Combat #{contador_pelea}");
            }
        }

        private void npcs_Dialogo_Recibido()
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is NpcBankAction nba || (cuenta.hasGroup && cuenta.group.lider.script.actions_manager.accion_actual is NpcBankAction))
            {
                if (cuenta.accountState != AccountStates.DIALOG)
                    return;

                IEnumerable<Npcs> npcs = cuenta.game.map.lista_npcs();
                Npcs npc = npcs.ElementAt((cuenta.game.character.hablando_npc_id * -1) - 1);
                cuenta.connexion.SendPacket("DR" + npc.pregunta + "|" + npc.respuestas[0], true);
            }
            else if (accion_actual is NpcAction || accion_actual is RespuestaAccion)
                acciones_Salida(400);
        }

        private void npcs_Dialogo_Acabado()
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is RespuestaAccion || accion_actual is CerrarVentanaAccion)
                acciones_Salida(200); 
        }

        public void enqueue_Accion(ScriptAction accion, bool iniciar_dequeue_acciones = false)
        {
            fila_acciones.Enqueue(accion);

            if (iniciar_dequeue_acciones)
                acciones_Salida(0);
        }

        public void get_Funcion_Personalizada(DynValue coroutine)
        {
            if (!cuenta.script.InExecution || coroutine_actual != null)
                return;

            coroutine_actual = manejador_script.script.CreateCoroutine(coroutine);
            procesar_Coroutine();
        }

        private void limpiar_Acciones()
        {
            while (fila_acciones.TryDequeue(out ScriptAction temporal)) { };
            accion_actual = null;
        }

        private void iniciar_Almacenamiento()
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is NpcBankAction)
                acciones_Salida(400);
        }

        private void cerrar_Almacenamiento()
        {
            if (!cuenta.script.InExecution)
                return;

            if (accion_actual is CerrarVentanaAccion)
                acciones_Salida(400);
        }

        private void procesar_Coroutine()
        {
            if (!cuenta.script.InExecution)
                return;

            try
            {
                DynValue result = coroutine_actual.Coroutine.Resume();

                if (result.Type == DataType.Void)
                    acciones_Funciones_Finalizadas();
            }
            catch (Exception ex)
            {
                cuenta.script.detener_Script(ex.ToString());
            }
        }

        private async Task procesar_Accion_Actual()
        {
            if (!cuenta.script.InExecution)
                return;

            string tipo = accion_actual.GetType().Name;

            switch (await accion_actual.process(cuenta))
            {
                case ResultadosAcciones.HECHO:
                    acciones_Salida(100);
                break;

                case ResultadosAcciones.FALLO:
                    cuenta.logger.log_Peligro("SCRIPT", $"{tipo} failed to process.");
                    cuenta.logger.log_Peligro("SCRIPT", $"{tipo} Stopping script..");
                    cuenta.script.detener_Script();
                    await Task.Delay(5000);
                    cuenta.logger.log_Peligro("SCRIPT", $"{tipo} Starting script in 5000 ms...");
                    cuenta.script.activar_Script();
                    cuenta.logger.log_Peligro("SCRIPT", $"{tipo} Script started...");
                    
               break;

                case ResultadosAcciones.PROCESANDO:
                    timer_out.Start();
                break;
            }
        }

        private void time_Out_Callback(object state)
        {
            if (!cuenta.script.InExecution)
                return;

            cuenta.logger.log_Peligro("SCRIPT", "Temps de finition");
            cuenta.script.detener_Script();
            cuenta.script.activar_Script();
        }

        private void acciones_Finalizadas()
        {
            if (mapa_cambiado)
            {
                mapa_cambiado = false;
                evento_accion_normal?.Invoke(true);
            }
            else
                evento_accion_normal?.Invoke(false);
        }

        private void acciones_Funciones_Finalizadas()
        {
            coroutine_actual = null;

            if (mapa_cambiado)
            {
                mapa_cambiado = false;
                evento_accion_personalizada?.Invoke(true);
            }
            else
                evento_accion_personalizada?.Invoke(false);
        }

        private void acciones_Salida(int delay) => Task.Factory.StartNew(async () =>
        {
            if (cuenta?.script.InExecution == false)
                return;

            if (timer_out.isEnabled)
                timer_out.Stop();

            if (delay > 0)
                await Task.Delay(delay);

            if (fila_acciones.Count > 0)
            {
                if (fila_acciones.TryDequeue(out ScriptAction accion))
                {
                    accion_actual = accion;
                    await procesar_Accion_Actual();
                }
            }
            else
            {
                if (coroutine_actual != null)
                    procesar_Coroutine();
                else
                    acciones_Finalizadas();
            }

        }, TaskCreationOptions.LongRunning);

        public void get_Borrar_Todo()
        {
            limpiar_Acciones();
            accion_actual = null;
            coroutine_actual = null;
            timer_out.Stop();

            contador_pelea = 0;
            contador_peleas_mapa = 0;
            contador_recoleccion = 0;
        }

        #region Zona Dispose
        public void Dispose() => Dispose(true);
        ~ActionsManager() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    timer_out.Dispose();
                }
                accion_actual = null;
                fila_acciones = null;
                cuenta = null;
                manejador_script = null;
                timer_out = null;
                disposed = true;
            }
        }
        #endregion
    }
}
