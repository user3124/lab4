using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

[Serializable]
public class TextFile : IOriginator
{
    public string FilePath { get; set; }
    public string Content { get; set; }

    public TextFile() { }

    public TextFile(string path)
    {
        FilePath = path;
        LoadContent();
    }

    private void LoadContent()
    {
        if (File.Exists(FilePath))
        {
            Content = File.ReadAllText(FilePath);
        }
        else
        {
            Content = string.Empty;
            File.WriteAllText(FilePath, Content);
        }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, Content);
    }

    // Бинарная сериализация
    public void BinarySerialize(string outputPath)
    {
        using (var stream = new FileStream(outputPath, FileMode.Create))
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
        }
    }

    public static TextFile BinaryDeserialize(string inputPath)
    {
        using (var stream = new FileStream(inputPath, FileMode.Open))
        {
            var formatter = new BinaryFormatter();
            return (TextFile)formatter.Deserialize(stream);
        }
    }

    // XML сериализация
    public void XmlSerialize(string outputPath)
    {
        using (var stream = new FileStream(outputPath, FileMode.Create))
        {
            var serializer = new XmlSerializer(typeof(TextFile));
            serializer.Serialize(stream, this);
        }
    }

    public static TextFile XmlDeserialize(string inputPath)
    {
        using (var stream = new FileStream(inputPath, FileMode.Open))
        {
            var serializer = new XmlSerializer(typeof(TextFile));
            return (TextFile)serializer.Deserialize(stream);
        }
    }

    // Реализация Memento
    public object GetMemento()
    {
        return new TextFileMemento
        {
            FilePath = this.FilePath,
            Content = this.Content
        };
    }

    public void SetMemento(object memento)
    {
        if (memento is TextFileMemento mem)
        {
            this.FilePath = mem.FilePath;
            this.Content = mem.Content;
            this.Save();
        }
    }

    [Serializable]
    private class TextFileMemento
    {
        public string FilePath { get; set; }
        public string Content { get; set; }
    }
}

public interface IOriginator
{
    object GetMemento();
    void SetMemento(object memento);
}

public class Caretaker
{
    private Stack<object> _undoStack = new Stack<object>();
    private Stack<object> _redoStack = new Stack<object>();

    public void SaveState(IOriginator originator)
    {
        _undoStack.Push(originator.GetMemento());
        _redoStack.Clear(); // Очищаем redo при новом действии
        Console.WriteLine("Состояние сохранено");
    }

    public void Undo(IOriginator originator)
    {
        if (_undoStack.Count > 0)
        {
            var memento = _undoStack.Pop();
            _redoStack.Push(originator.GetMemento()); // Сохраняем текущее состояние в redo
            originator.SetMemento(memento);
            Console.WriteLine("Изменение отменено");
        }
        else
        {
            Console.WriteLine("Нечего отменять");
        }
    }

    public void Redo(IOriginator originator)
    {
        if (_redoStack.Count > 0)
        {
            var memento = _redoStack.Pop();
            _undoStack.Push(originator.GetMemento()); // Сохраняем текущее состояние в undo
            originator.SetMemento(memento);
            Console.WriteLine("Изменение возвращено");
        }
        else
        {
            Console.WriteLine("Нечего возвращать");
        }
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
}

public class TextFileFinder
{
    public List<string> FindFiles(string directory, string[] keywords)
    {
        return Directory.GetFiles(directory, "*.txt")
                       .Where(file => keywords.Any(keyword =>
                           File.ReadAllText(file).Contains(keyword)))
                       .ToList();
    }
}

public class TextFileIndexer
{
    public Dictionary<string, List<string>> Index { get; } = new Dictionary<string, List<string>>();

    public void BuildIndex(string directory, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            Index[keyword] = Directory.GetFiles(directory, "*.txt")
                                     .Where(file => File.ReadAllText(file).Contains(keyword))
                                     .ToList();
        }
    }
}

public class TextEditor
{
    private TextFile _currentFile;
    private readonly Caretaker _caretaker = new Caretaker();

    public void OpenFile(string path)
    {
        _currentFile = new TextFile(path);
        _caretaker.SaveState(_currentFile);
    }

    public void Edit(string newContent)
    {
        if (_currentFile != null)
        {
            _caretaker.SaveState(_currentFile);
            _currentFile.Content = newContent;
            SaveToFile(); // Сохраняем изменения на диск
        }
    }

    public void Undo()
    {
        if (_currentFile != null)
        {
            _caretaker.Undo(_currentFile);
            SaveToFile(); // Сохраняем отменённое состояние на диск
        }
    }

    public void Redo()
    {
        if (_currentFile != null)
        {
            _caretaker.Redo(_currentFile);
            SaveToFile(); // Сохраняем возвращённое состояние на диск
        }
    }

    public void ShowContent()
    {
        if (_currentFile != null)
        {
            Console.WriteLine(_currentFile.Content);
        }
    }

    private void SaveToFile()
    {
        _currentFile?.Save(); // Используем метод Save класса TextFile
    }

    public bool CanUndo => _caretaker.CanUndo;
    public bool CanRedo => _caretaker.CanRedo;
}

class Program
{
    static void Main(string[] args)
    {
        var editor = new TextEditor();
        var finder = new TextFileFinder();
        var indexer = new TextFileIndexer();

        while (true)
        {
            Console.WriteLine("\n1. Редактировать файл");
            Console.WriteLine("2. Поиск файлов по ключевым словам");
            Console.WriteLine("3. Индексация файлов");
            Console.WriteLine("4. Выход");
            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    EditorMenu(editor);
                    break;
                case "2":
                    SearchMenu(finder);
                    break;
                case "3":
                    IndexMenu(indexer);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Неверный выбор");
                    break;
            }
        }
    }

    static void EditorMenu(TextEditor editor)
    {
        Console.Write("Введите путь к файлу: ");
        var path = Console.ReadLine();
        editor.OpenFile(path);

        while (true)
        {
            Console.WriteLine("\n1. Просмотреть содержимое");
            Console.WriteLine("2. Редактировать");
            Console.WriteLine("3. Отменить (Undo)");
            Console.WriteLine("4. Вернуть (Redo)");
            Console.WriteLine("5. Вернуться в главное меню");
            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    editor.ShowContent();
                    break;
                case "2":
                    Console.Write("Введите новый текст: ");
                    editor.Edit(Console.ReadLine());
                    break;
                case "3":
                    editor.Undo();
                    break;
                case "4":
                    editor.Redo();
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Неверный выбор");
                    break;
            }

            // Показываем статус доступных операций
            Console.WriteLine($"\nСтатус: Undo доступен: {editor.CanUndo}, Redo доступен: {editor.CanRedo}");
        }
    }

    static void SearchMenu(TextFileFinder finder)
    {
        Console.Write("Введите директорию для поиска: ");
        var dir = Console.ReadLine();

        Console.Write("Введите ключевые слова через запятую: ");
        var keywords = Console.ReadLine().Split(',');

        var files = finder.FindFiles(dir, keywords);

        Console.WriteLine("\nНайденные файлы:");
        foreach (var file in files)
        {
            Console.WriteLine(file);
        }
    }

    static void IndexMenu(TextFileIndexer indexer)
    {
        Console.Write("Введите директорию для индексации: ");
        var dir = Console.ReadLine();

        Console.Write("Введите ключевые слова через запятую: ");
        var keywords = Console.ReadLine().Split(',');

        indexer.BuildIndex(dir, keywords);

        Console.WriteLine("\nРезультат индексации:");
        foreach (var entry in indexer.Index)
        {
            Console.WriteLine($"Ключевое слово: {entry.Key}");
            foreach (var file in entry.Value)
            {
                Console.WriteLine($"  {file}");
            }
        }
    }
}