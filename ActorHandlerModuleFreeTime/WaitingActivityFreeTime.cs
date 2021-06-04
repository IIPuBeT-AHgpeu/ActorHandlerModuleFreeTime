using System;

using ActorModule;
using InitializeActorModule;

namespace ActorHandlerModuleFreeTime
{
    class WaitingActivityFreeTime : IActivity
    {
        public int Priority { get; set; }
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
            SecondsToUpdate += deltaTime;

            if (SecondsToUpdate >= 1)
            {
                // Если мы в месте, где можно покушать
                if (TagKey == "shop")
                {
                    // Кушаем
                    if (actor.GetState<SpecState>().Hunger > 0.995 * 100) actor.GetState<SpecState>().Hunger = 100;
                    else actor.GetState<SpecState>().Hunger += 0.005 * 100;
                    actor.GetState<SpecState>().Money -= 1;
                }
                else
                {
                    // Не кушаем
                    if (actor.GetState<SpecState>().Hunger <= 0.001 * 100) actor.GetState<SpecState>().Hunger = 0;
                    else actor.GetState<SpecState>().Hunger -= 0.001 * 100;
                }
                // Веселимся и устаем :)
                if (actor.GetState<SpecState>().Mood > 0.995 * 100) actor.GetState<SpecState>().Mood = 100;
                else actor.GetState<SpecState>().Mood += 0.005 * 100;
                if (actor.GetState<SpecState>().Fatigue <= 0.001 * 100) actor.GetState<SpecState>().Fatigue = 0;
                else actor.GetState<SpecState>().Fatigue -= 0.001 * 100;

                SecondsToUpdate -= 1;
            }
#if DEBUG
            Console.WriteLine($"Hunger: {actor.GetState<SpecState>().Hunger}; Mood: {actor.GetState<SpecState>().Mood}; Fatigue: {actor.GetState<SpecState>().Fatigue} Tag: {TagKey}");
#endif
            return false;
        }
    }
}
