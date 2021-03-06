using System;

using ActorModule;
using InitializeActorModule;

namespace ActorHandlerModuleFreeTime
{
    class WaitingActivityFreeTime : IActivity
    {
        public int Priority { get; private set; }
        // Тег места - для проверки: можно ли покушать в данном месте
        private string TagKey { get; set; }
        private double SecondsToUpdate { get; set; }
        public WaitingActivityFreeTime(int priority, string tagKey)
        {
            TagKey = tagKey;
            Priority = priority;
            SecondsToUpdate = 0;
        }
        public WaitingActivityFreeTime(int priority, string tagKey, double secondsFromPreActivity)
        {
            TagKey = tagKey;
            Priority = priority;
            SecondsToUpdate = secondsFromPreActivity;
        }
        public bool Update(Actor actor, double deltaTime)
        {
#if DEBUG
            Console.WriteLine("Actor is waiting (freetime).");
#endif
            SecondsToUpdate += deltaTime;

            if (SecondsToUpdate >= 1)
            {
                // Если мы в месте, где можно покушать
                if (TagKey == "shop")
                {
                    // Кушаем
                    if (actor.GetState<SpecState>().Satiety > 0.995 * 100) actor.GetState<SpecState>().Satiety = 100;
                    else actor.GetState<SpecState>().Satiety += 0.005 * 100;
                    actor.GetState<SpecState>().Money -= 1;
                }
                else
                {
                    // Не кушаем
                    if (actor.GetState<SpecState>().Satiety <= 0.001 * 100) actor.GetState<SpecState>().Satiety = 0;
                    else actor.GetState<SpecState>().Satiety -= 0.001 * 100;
                }
                // Веселимся и устаем :)
                if (actor.GetState<SpecState>().Mood > 0.995 * 100) actor.GetState<SpecState>().Mood = 100;
                else actor.GetState<SpecState>().Mood += 0.005 * 100;
                if (actor.GetState<SpecState>().Stamina <= 0.001 * 100) actor.GetState<SpecState>().Stamina = 0;
                else actor.GetState<SpecState>().Stamina -= 0.001 * 100;

                SecondsToUpdate -= 1;
            }
#if DEBUG
            //Console.WriteLine($"Hunger: {actor.GetState<SpecState>().Stamina}; Mood: {actor.GetState<SpecState>().Mood}; Fatigue: {actor.GetState<SpecState>().Stamina} Tag: {TagKey}");
#endif
            // Определяем текущий приоритет для активностей FreeTime
            if (actor.GetState<SpecState>().Mood <= (0.05 * 100)) Priority = 92;
            else if (actor.GetState<SpecState>().Mood > (0.05 * 100) && actor.GetState<SpecState>().Mood <= (0.1 * 100)) Priority = 82;
            else if (actor.GetState<SpecState>().Mood > (0.1 * 100) && actor.GetState<SpecState>().Mood <= (0.3 * 100)) Priority = 62;
            else if (actor.GetState<SpecState>().Mood > (0.3 * 100) && actor.GetState<SpecState>().Mood <= (0.6 * 100)) Priority = 42;
            else if (actor.GetState<SpecState>().Mood > (0.6 * 100) && actor.GetState<SpecState>().Mood <= (0.8 * 100)) Priority = 22;
            else if (actor.GetState<SpecState>().Mood > 0.8 * 100) Priority = 2;

            return false;
        }
    }
}
