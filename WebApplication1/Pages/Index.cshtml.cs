// Подключаем нужные библиотеки (пространства имён)
// Они дают нам доступ к готовым классам и методам.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;


// Пространство имён, объединяет классы проекта логически.
// Здесь у нас все классы страниц в папке Pages будут в WebApplication1.Pages
namespace WebApplication1.Pages
{
    // Класс ProposalRequest — это "модель данных" (DTO).
    // Он описывает то, что вводит пользователь в форму.
    public class ProposalRequest
    {
        //ID
        public int Id { get; set; }
        //Тип клиента
        public string CustomerType { get; set; }
        //Дата создания
        public DateTime? CreatedAt { get; set; }
        // Свойство: строка для ФИО
        public string CustomerName { get; set; }
        // Свойство: число (double), площадь объекта
        public double Area { get; set; }
        // Свойство: булево значение (true/false), нужен ли дизайн
        public bool Design { get; set; }
    }


    // Класс IndexModel — "модель страницы" (PageModel).
    // Он обрабатывает логику для Razor Page Index.cshtml
    public class IndexModel : PageModel
    {
        // Атрибут [BindProperty] означает: данные из формы будут
        // автоматически "привязаны" к этому свойству Proposal.
        [BindProperty]
        public ProposalRequest Proposal { get; set; }
        public List<ProposalRequest> AllProposals { get; set; }
        // Свойство для вывода сообщения (например, предпросмотра).
        public string ResultMessage { get; set; }

        //Приватный вспомогательный метод для подключения БД
        private SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection("Data source = proposals.db");
            connection.Open();
            return connection;
        }

        public void OnGet()
        {
            AllProposals = GetAllProposals();
        }
        // Метод-обработчик POST-запроса с кнопки "Предпросмотр".
        // Возвращает IActionResult — результат, который вернётся в браузер.
        public IActionResult OnPostPreview()
        {
            if (Proposal != null) // проверяем, что пользователь что-то отправил
            {
                // Формируем текст предпросмотра
                ResultMessage = 
                    $"Имя: {Proposal.CustomerName}\n+" +
                    $"Площадь: {Proposal.Area}м²\n+" +
                    $"Дизайн: {(Proposal.Design ? "Да":"Нет")}"+
                    $"Тип заказчика: {(Proposal.CustomerType)}\n"+
                    $"Дата создания: {(Proposal.CreatedAt)}";
                
            }
            else
            {
                ResultMessage = "Ошибка: Нужно заполнить данные";
            }

            // Возвращаем страницу заново с этим текстом
            AllProposals = GetAllProposals();
            return Page();
        }

        // Метод-обработчик POST-запроса с кнопки "Скачать PDF".
        // Почти как предпросмотр, но вместо текста формируем PDF.
        public IActionResult OnPostDownload()
        {
            if (Proposal != null)
            {
                //Дата создания

                if (Proposal.CreatedAt == null)
                {  
                    Proposal.CreatedAt = DateTime.Now;
                }

                //Сначала сохраним данные в БД:
                AddProposalToDatabase(Proposal);

                // Вызываем отдельный метод, который генерирует PDF.
                var pdfBytes = GenerateProposalPDF(Proposal);

                // Возвращаем файл клиенту (браузеру).
                return File(pdfBytes, "application/pdf", "KPFromPBK.pdf");
            }
            else 
            {
                ResultMessage = "Ошибка: Нужно заполнить данные";
                return Page();
            }
        }
        //Метод для кнопки просмотра БД с клиентами
        public IActionResult OnPostList()
        {
            //читаем из Бд
            AllProposals = GetAllProposals();
            //возвращаем страницу
            return Page();
        }
        public void AddProposalToDatabase(ProposalRequest proposal)
        {
            //строка подключения к файлу БД (путь к файлу .db)
            //обновлено: теперь отдельный метод для подключения
            //string connectionstring = "data source = proposals.db";

            //Метод, который корректно добавляет дату ы AddProposalDatabase
            proposal.CreatedAt = proposal.CreatedAt ?? DateTime.Now;

            using (var connection = CreateConnection())
            { 
            connection.Open();

                //Создаем SQL-команду
                var command = connection.CreateCommand();
                command.CommandText =
                    @"INSERT INTO Proposals (CustomerName, Area, Design, CustomerType, CreatedAt)
                      VALUES ($name, $area, $design, $CustomerType, $CreatedAt)";

                //параметры, чтобы избежать SQL-инъекций
                command.Parameters.AddWithValue("$name", proposal.CustomerName ?? "Неизвестно");
                command.Parameters.AddWithValue("$area", proposal.Area);
                command.Parameters.AddWithValue("$design", proposal.Design ? 1 : 0);
                command.Parameters.AddWithValue("$CustomerType",
                    string.IsNullOrEmpty(proposal.CustomerType) ? "Не указан" : proposal.CustomerType);
                command.Parameters.AddWithValue(
                    "$CreatedAt", 
                    proposal.CreatedAt.HasValue
                    ? proposal.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
                );

                //Временная отладка
                Console.WriteLine("DEBUG:");
                Console.WriteLine($"Name: {proposal.CustomerName}");
                Console.WriteLine($"Area: {proposal.Area}");
                Console.WriteLine($"Design: {proposal.Design}");
                Console.WriteLine($"CustomerType: {proposal.CustomerType}");
                Console.WriteLine($"CreatedAt: {proposal.CreatedAt}");
                //выполняем вставку
                command.ExecuteNonQuery();
            }
        }

        //Метод для получения всех заявок из БД
        public List<ProposalRequest> GetAllProposals()
        { 
            //Создаем пустой список, куда будем складывать все записи из БД 
            var list = new List<ProposalRequest>();

            //Открываем соединение с базой proposal.bd
            //обновлено: теперь отдельный метод для подключения
            using (var connection = CreateConnection())
            {
                //физически открываем БД
                connection.Open();

                //Создаем SQL-команду, которая достанет все строки из таблдицы Proposal
                using (var command = new SqliteCommand("SELECT CustomerName, Area, Design, CustomerType, CreatedAt FROM Proposals", connection))
                //Выполняем команду и получаем читалку строк
                using (var reader = command.ExecuteReader())

                {
                    //Получаем индексы колонок БД один раз
                    int idxName = reader.GetOrdinal("CustomerName");
                    int idxArea = reader.GetOrdinal("Area");
                    int idxDesign = reader.GetOrdinal("Design");
                    int idxType = reader.GetOrdinal("CustomerType");
                    int idxCreated = reader.GetOrdinal("CreatedAt");

                    //пока есть строки, читаем каждую по очереди
                    while (reader.Read())
                    {
                        //Создаем новый объект ProposaRequest (класс DTO)
                        //и заполняем его полями из текущей строки
                        list.Add(new ProposalRequest
                        {
                            CustomerName = reader.GetString(idxName),//Первый столбец
                            Area = reader.GetDouble(idxArea),//Второй столбец
                            Design = reader.GetBoolean(idxDesign),//Третий столбец
                            CustomerType = reader.IsDBNull(idxType) ? null : reader.GetString(idxType),
                            CreatedAt = reader.IsDBNull(idxCreated) 
                                ? null
                                : DateTime.Parse(reader.GetString(idxCreated))
                        });
                    }
                }
            }
            // Для отладки выводим в Output Visual Studio все полученные заявки
            foreach (var p in list)
                Console.WriteLine($"{p.CustomerName} | {p.Area} | {p.Design}");

            //Возвращаем список заявок вызывающему коду
            return list;
        }
        // Метод GenerateProposalPDF — создаёт PDF из данных.
        // Принимает ProposalRequest (данные из формы),
        // возвращает массив байт (готовый PDF).
        public byte[] GenerateProposalPDF(ProposalRequest proposal)
        {
            // Создаём документ QuestPDF с помощью Fluent API
            var pdf = Document.Create(container =>
            {
                // Каждая страница документа
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    // Заголовок страницы
                    page.Header()
                        .Text("Коммерческое предложение")
                        .SemiBold().FontSize(20).AlignCenter();

                    // Основное содержимое страницы
                    page.Content()
                        .Text($"Заказчик: {proposal.CustomerName}\n" +
                              $"Площадь: {proposal.Area} м²\n" +
                              $"Дизайн интерьера: {proposal.Design}\n"+
                              $"Тип заказчика: {proposal.CustomerType}\n"+
                              $"Дата создания: {proposal.CreatedAt}");

                    // Нижний колонтитул (footer)
                    page.Footer()
                        .AlignCenter()
                        .Text("Kvadrat © 2025");
                });
            });

            // Генерируем PDF и возвращаем его как массив байтов
            return pdf.GeneratePdf();
        }
    }
}
