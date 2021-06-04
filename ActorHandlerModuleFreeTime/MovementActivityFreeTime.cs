using System;
using System.Collections.Generic;

using ActorModule;
using InitializeActorModule;
using PathsFindingCoreModule;
using NetTopologySuite.Geometries;  // Отсюда Point и другая геометрия 
using NetTopologySuite.Mathematics; // Отсюда векторы 

namespace ActorHandlerModuleFreeTime
{
    class MovementActivityFreeTime : IActivity
    {
        public Coordinate[] Path;
        public int i = 0;

        public bool IsPath = true;
        private double SecondsToUpdate { get; set; }

        // Приоритет делаем авто-свойством, со значением по умолчанию
        // Вообще он дожен был быть полем, но интерфейсы не дают объявлять поля, так что...
        public int Priority { get; set; }

        // Точка назначения
        public Place Destination { get; set; }

        public MovementActivityFreeTime(Actor actor)
        {
            Destination = actor.GetState<PlaceState>().FavoritePlaces[ChoosePlace(actor)];
        }

        public MovementActivityFreeTime(Actor actor, int priority)
        {
            Destination = actor.GetState<PlaceState>().FavoritePlaces[ChoosePlace(actor)];
            Priority = priority;
        }

        static private int ChoosePlace(Actor actor)
        {
            const double FoodPrice = 100; //Установил цену на еду

            Random rnd = new Random();  //Необходимо перенести в конец перед return-ом

            List<double> WayLenghtList = new List<double>() { };    //Лист, содержащий длины путей
            List<int> ListOfChoose = new List<int>() { };           //Лист, содержащий набор индексов списка любимых мест для выбора 1 из них
            bool ActorHaveFavoriteFoodPlaceFlag = false;            //Флаг присутствия мест для перекуса

            //Считаем расстояние до каждого из любимых мест
            for (int i = 0; i < actor.GetState<PlaceState>().FavoritePlaces.Count; i++)
            {
                ListOfChoose.Add(i);
                WayLenghtList.Add(PathsFinding.GetPath(new Coordinate(actor.X, actor.Y), new Coordinate(actor.GetState<PlaceState>().FavoritePlaces[i].X,
                    actor.GetState<PlaceState>().FavoritePlaces[i].Y), "Walking").Result.Length);  //Считаем длину пути 
                if (actor.GetState<PlaceState>().FavoritePlaces[i].TagKey == "shop" && !ActorHaveFavoriteFoodPlaceFlag)   //Поднять флаг присутствия мест для перекуса
                    ActorHaveFavoriteFoodPlaceFlag = true;
            }

            //Переменные-буферы для сортировки
            double tmpDouble = 0;
            int tmpInt = 0;

            //Сортировка по расстоянию до места
            //сортирует сразу 2 списка, сохраняя соответствие
            for (int i = 0; i < WayLenghtList.Count - 1; i++)
            {
                for (int j = i + 1; j < WayLenghtList.Count; j++)
                {
                    if (WayLenghtList[i] > WayLenghtList[j])
                    {
                        tmpDouble = WayLenghtList[i];
                        tmpInt = ListOfChoose[i];
                        WayLenghtList[i] = WayLenghtList[j];
                        ListOfChoose[i] = ListOfChoose[j];
                        WayLenghtList[j] = tmpDouble;
                        ListOfChoose[j] = tmpInt;
                    }
                }
            }

            /*
                Далее необходимо усовершенствовать список индексов: необходимо увеличить шанс выбора наиболее привлекательных  
                мест, исходя из текущего состояния параметров Actor'a
            */

            //Если стоит покушать (Насыщенность <= 65%) и не очень устали и есть места, чтобы перекусить
            if (actor.GetState<SpecState>().Hunger <= 0.65 * 100 && actor.GetState<SpecState>().Money >= 2 * FoodPrice && actor.GetState<SpecState>().Fatigue > 0.3 * 100 /*>?*/ && ActorHaveFavoriteFoodPlaceFlag)
            {
                //модифицируем список выбора места: чем ближе место, в котором можно покушать, тем выше вероятность
                //оставшееся место (где нет возможности поесть) имеет или самый низкий шанс или (возможно) не самый,
                //но один из самых низких шансов

                //Флаг присутствия места, не являющимся местом перекуса
                bool PresenceFlag = false;

                //Модифицируем массив индексов: убираем все места, в которых нет возможности поесть, кроме одного (самого ближайшего)
                for (int i = 0; i < ListOfChoose.Count; i++)
                {
                    if (actor.GetState<PlaceState>().FavoritePlaces[ListOfChoose[i]].TagKey != "shop")
                    {
                        if (!PresenceFlag)
                        {
                            PresenceFlag = true;
                        }
                        else
                        {
                            ListOfChoose.RemoveAt(i);
                            WayLenghtList.RemoveAt(i);
                            i--;
                        }
                    }
                }

                int ListLength = ListOfChoose.Count;        //длина списка до добавлений повторяющихся индексов
                int IfFoodPlaceAdditional = ListLength * 2; //количество добавляемых элементов для случая, когда в данном месте можно покушать

                for (int i = 0; i < ListLength; i++)
                {
                    if (actor.GetState<PlaceState>().FavoritePlaces[ListOfChoose[i]].TagKey == "shop")
                    {
                        for (int j = 0; j < IfFoodPlaceAdditional; j++) ListOfChoose.Add(ListOfChoose[i]);
                        IfFoodPlaceAdditional--;
                    }
                    else
                    {
                        for (int j = 0; j < (IfFoodPlaceAdditional - ListLength) / 2; j++) ListOfChoose.Add(ListOfChoose[i]);
                    }
                }

            }

            else if (actor.GetState<SpecState>().Fatigue > 0.3 * 100)  //если кушать не нужно или нет мест, чтобы перекусить, и не сильно устали
            {
                /*
                  модифицируем список выбора: чем ближе место, тем вероятнее в него попасть
                */

                for (int i = 0; i < WayLenghtList.Count; i++)
                {
                    for (int j = WayLenghtList.Count - i - 1; j > 0; j--)
                    {
                        ListOfChoose.Add(ListOfChoose[i]);
                    }
                }
            }

            else //если сильно устали
            {
                //модифицируем список: убираем половину мест, до которых дальше всего идти, и, в оставшемся массиве,
                //распределяем вероятности: чем ближе место, тем выше вероятность в него попасть

                //Убираем из списка выбора ту половину, которая имеет наибольшие расстояния
                for (int i = 0; i < WayLenghtList.Count / 2; i++)
                {
                    ListOfChoose.RemoveAt(ListOfChoose.Count - 1);
                }

                //Добавляем вероятности выбора следующим образом: чем ближе место, тем сильнее нужно увеличивать вероятность
                int AdditionalProbability = WayLenghtList.Count;    //количество добавляемых индексов
                int ListLength = ListOfChoose.Count;                //длина листа до добавления индексов

                for (int i = 0; i < ListLength; i++)
                {
                    for (int j = 0; j < AdditionalProbability; j++)
                    {
                        ListOfChoose.Add(ListOfChoose[i]);
                    }
                    AdditionalProbability /= 2;
                }
            }

            return ListOfChoose[rnd.Next(0, ListOfChoose.Count)];

        }

        // Здесь происходит работа с актором
        public bool Update(Actor actor, double deltaTime)
        {           
            // Расстояние, которое может пройти актор с заданной скоростью за прошедшее время
            double distance = actor.GetState<SpecState>().Speed * deltaTime;

            SecondsToUpdate += deltaTime;

            if (SecondsToUpdate >= 1)
            {
                // Уменьшаем параметр голода, усталости, настроения
                if (actor.GetState<SpecState>().Hunger <= 0.001 * 100) actor.GetState<SpecState>().Hunger = 0;
                else actor.GetState<SpecState>().Hunger -= 0.001 * 100;

                if (actor.GetState<SpecState>().Fatigue <= 0.001 * 100) actor.GetState<SpecState>().Fatigue = 0;
                else actor.GetState<SpecState>().Fatigue -= 0.001 * 100;

                SecondsToUpdate -= 1;
            }
            // Определяем текущий приоритет для активностей FreeTime
            if (actor.GetState<SpecState>().Mood <= (0.05 * 100)) Priority = 92;
            else if (actor.GetState<SpecState>().Mood > (0.05 * 100) && actor.GetState<SpecState>().Mood <= (0.1 * 100)) Priority = 82;
            else if (actor.GetState<SpecState>().Mood > (0.1 * 100) && actor.GetState<SpecState>().Mood <= (0.3 * 100)) Priority = 62;
            else if (actor.GetState<SpecState>().Mood > (0.3 * 100) && actor.GetState<SpecState>().Mood <= (0.6 * 100)) Priority = 42;
            else if (actor.GetState<SpecState>().Mood > (0.6 * 100) && actor.GetState<SpecState>().Mood <= (0.8 * 100)) Priority = 22;
            else if (actor.GetState<SpecState>().Mood > 0.8 * 100) Priority = 2;
#if DEBUG
            Console.WriteLine($"Hunger: {actor.GetState<SpecState>().Hunger}; Mood: {actor.GetState<SpecState>().Mood}; Fatigue: {actor.GetState<SpecState>().Fatigue}");
#endif
            if (IsPath)
            {
                var firstCoordinate = new Coordinate(actor.X, actor.Y);
                var secondCoordinate = new Coordinate(Destination.X, Destination.Y);

                Path = PathsFinding.GetPath(firstCoordinate, secondCoordinate, "Walking").Result.Coordinates;
                IsPath = false;
            }

            Vector2D direction = new Vector2D(actor.Coordinate, Path[i]);
            // Проверка на перешагивание

            if (direction.Length() <= distance)
            {
                // Шагаем в точку, если она ближе, чем расстояние которое можно пройти
                actor.X = Path[i].X;
                actor.Y = Path[i].Y;
            }
            else
            {
                // Вычисляем новый вектор, с направлением к точке назначения и длинной в distance
                direction = direction.Normalize().Multiply(distance);

                // Смещаемся по вектору
                actor.X += direction.X;
                actor.Y += direction.Y;
            }

            if (actor.X == Path[i].X && actor.Y == Path[i].Y && i < Path.Length - 1)
            {
                i++;
#if DEBUG
                Console.WriteLine(i);
                Console.WriteLine(Path.Length);
#endif
            }

            // Если в процессе шагания мы достигли точки назначения
            if (actor.X == Path[Path.Length - 1].X && actor.Y == Path[Path.Length - 1].Y)
            {
                Console.WriteLine("Start WaitingFreeTime");
                i = 0;
                IsPath = true;
                actor.Activity = new WaitingActivityFreeTime(Priority, Destination.TagKey, SecondsToUpdate);
                Priority = 0;
                //return true;
            }
            return false;
        }
    }
}
