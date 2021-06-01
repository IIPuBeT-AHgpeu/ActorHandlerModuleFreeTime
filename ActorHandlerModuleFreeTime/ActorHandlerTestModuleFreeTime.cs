using System;
using OSMLSGlobalLibrary.Modules;
using ActorModule;
using InitializeActorModule;

namespace ActorHandlerModuleFreeTime
{
    class ActorHandlerTestModuleFreeTime : OSMLSModule
    {
        /// <summary>
        /// Инициализация модуля. В отладочной конфигурации выводит сообщение
        /// </summary>
        protected override void Initialize()
        {
#if DEBUG
            Console.WriteLine("ActorHandlerTestModuleFreeTime: Initialize");
#endif
        }
        /// <summary>
        /// Вызывает Update на всех акторах
        /// </summary>
        public override void Update(long elapsedMilliseconds)
        {
            // Получаем список акторов
            var actors = MapObjects.GetAll<Actor>();
            // Console.WriteLine($"Got {actors.Count} actors\n");

            // Для каждого актора проверяем условия и назначаем новую активность если нужно
            foreach (var actor in actors)
            {
                int newPriority = 0;

                // Определяем текущий приоритет для активностей FreeTime
                if (actor.GetState<SpecState>().Mood <= 0.05 * 100) newPriority = 92;
                else if (actor.GetState<SpecState>().Mood > (0.05 * 100) && actor.GetState<SpecState>().Mood <= 0.1 * 100) newPriority = 82;
                else if (actor.GetState<SpecState>().Mood > 0.1 * 100 && actor.GetState<SpecState>().Mood <= 0.3 * 100) newPriority = 62;
                else if (actor.GetState<SpecState>().Mood > 0.3 * 100 && actor.GetState<SpecState>().Mood <= 0.6 * 100) newPriority = 42;
                else if (actor.GetState<SpecState>().Mood > 0.6 * 100 && actor.GetState<SpecState>().Mood <= 0.8 * 100) newPriority = 22;
                else if (actor.GetState<SpecState>().Mood > 0.8 * 100) newPriority = 2;

                // Есть ли активность
                bool isActivity = actor.Activity != null;

                // Относятся ли активности к FreeTime
                bool isFreeTimeMovementActivity = actor.Activity is MovementActivityFreeTime;
                bool isFreeTimeWaitingActivity = actor.Activity is WaitingActivityFreeTime;

                Console.WriteLine($"Flags: {isActivity} {isFreeTimeMovementActivity} {isFreeTimeWaitingActivity}");

                // Если вообще нет активности
                // или (активности не FreeTime и приоритет активностей FreeTime выше приоритета текущей активности)
                if ((!isActivity) || (!isFreeTimeMovementActivity && !isFreeTimeWaitingActivity && newPriority > actor.Activity.Priority))
                {
                    // Назначить актору путь до работы
                    actor.Activity = new MovementActivityFreeTime(actor, newPriority);
                    Console.WriteLine("Said actor go walking\n");
                }

            }
        }
    }
}
