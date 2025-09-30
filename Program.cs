using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleApp1
{
    class Program
    {
        private static TelegramBotClient botClient; 
        private static Dictionary<long, string> userStates = new Dictionary<long, string>(); 
        private static Dictionary<long, int> userRandomNumbers = new Dictionary<long, int>();
        private static Random rnd = new Random();

        static async Task Main(string[] args)
        {
            using var finish = new CancellationTokenSource();
            var bot = new TelegramBotClient("8391295722:AAGLx615DKbZyOJ6hVx4hSC19w6zBS6sq2o", cancellationToken: finish.Token);

            bot.OnError += OnError;
            bot.OnMessage += OnMessage;
            bot.OnUpdate += OnUpdate;

            Console.WriteLine("Нажмите Enter чтобы завершить работу");
            Console.ReadLine();

            finish.Cancel();

            async Task OnMessage(Message message, UpdateType type)
            {
                if (message.Text == "/start")
                {
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Простые числа в диапазоне", "prost_range"),
                            InlineKeyboardButton.WithCallbackData("Рандом числа в диапазоне", "random_range")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Угадай число", "guess_range")
                        }
                    });

                    await bot.SendMessage(message.Chat, "Привет брат! Выбери один из вариантов в которые ты хочешь поиграть", replyMarkup: inlineKeyboard);
                }

                else if (userStates.TryGetValue(message.From.Id, out string state))
                {
                    if (state == "preserving_guess_number")
                    {
                        string userInput = message.Text;
                        if (int.TryParse(userInput, out int userGuess))
                        {
                            if (userRandomNumbers.TryGetValue(message.From.Id, out int secretNumber))
                            {
                                if (userGuess == secretNumber)
                                {
                                    await bot.SendMessage(message.Chat, $"Поздравляю! Ты угадал число {secretNumber}!");
                                    userRandomNumbers.Remove(message.From.Id);
                                    userStates.Remove(message.From.Id);
                                }
                                else if (userGuess < secretNumber)
                                {
                                    await bot.SendMessage(message.Chat, "Больше!"); 
                                }
                                else
                                {
                                    await bot.SendMessage(message.Chat, "Меньше!");
                                }
                            }
                        }
                        else
                        {
                            await bot.SendMessage(message.Chat, "Введи число, а не текст!");
                        }
                        return; 
                    }
                    var range = ParseRange(message.Text);

                    if (range == null)
                    {
                        await bot.SendMessage(message.Chat, "Неверный формат! Введите например: \"10 до 40\" или \"10 40\"");
                        return;
                    }

                    int min = range.Value.min;
                    int max = range.Value.max;

                    switch (state)
                    {
                        case "preserving_prost_range": 
                            await bot.SendMessage(message.Chat, $"Принял диапазон для простых чисел: от {min} до {max}");
                            userStates.Remove(message.From.Id); 
                            break;

                        case "preserving_random_range": 
                            int randomNumber2 = rnd.Next(min, max + 1);
                            await bot.SendMessage(message.Chat, $"Диапазон: {min}-{max}\nСлучайное число: {randomNumber2}");
                            userStates.Remove(message.From.Id); 
                            break;

                        case "preserving_guess_range": 
                            await bot.SendMessage(message.Chat, $"Диапазон для угадывания: от {min} до {max}");
                            int randomNumber3 = rnd.Next(min, max + 1);
                            userRandomNumbers[message.From.Id] = randomNumber3;
                            userStates[message.From.Id] = "preserving_guess_number";
                            await bot.SendMessage(message.Chat, "Введите число"); 
                            break;
                    }
                }
            }

            async Task OnUpdate(Update update)
            {
                if (update is { CallbackQuery: { } query })
                {
                    await bot.AnswerCallbackQuery(query.Id, $"Ты выбрал {query.Data}");

                    if (query.Data == "prost_range") 
                    {
                        userStates[query.From.Id] = "preserving_prost_range"; 
                        await bot.SendMessage(query.Message!.Chat, "Введите диапазон чисел (формат: 10 до 40)");
                    }
                    else if (query.Data == "random_range") 
                    {
                        userStates[query.From.Id] = "preserving_random_range"; 
                        await bot.SendMessage(query.Message!.Chat, "Введите диапазон для случайных чисел (формат: 1 до 100)");
                    }
                    else if (query.Data == "guess_range") 
                    {
                        userStates[query.From.Id] = "preserving_guess_range"; 
                        await bot.SendMessage(query.Message!.Chat, "Введите диапазон для игры 'Угадай число' (формат: 1 до 50)");
                    }
                }
            }


            async Task OnError(Exception exception, HandleErrorSource source)
            {
                Console.WriteLine(exception);
            }
        }

            private static (int min, int max)? ParseRange(string input)
            {
                try
                {
                    string cleaned = input.ToLower()
                        .Replace("от", "")  
                        .Replace("до", "")  
                        .Replace("-", " ") 
                        .Replace("по", "")  
                        .Trim();           

                    // Разбиваем строку на части по пробелам и запятым
                    string[] parts = cleaned.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))   
                    {
                        if (min <= max)
                            return (min, max); 
                    }
                }
                catch
                {
                    // диапазон не распознан
                    return null;
                }
                return null; 
            }
    }
}

