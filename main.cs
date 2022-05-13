using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;


public class Point
{
    public int X;
    public int Y;
}

public class Game
{
    public readonly int[,] field;
    public readonly Point zeroPoint;
    private int hashCode;
    private string print="";

    public Game(int [,] field)
    {
        this.field = field;
        zeroPoint = GetZeroPoint();
        hashCode = field.Cast<int>().Aggregate((a, b) => a * 97 + b);
    }
    
    private Point GetZeroPoint()
    {
        return Rectangle.Enumer(field.GetLength(0), field.GetLength(1))
                        .Where(a => field[a.X, a.Y] == 0).FirstOrDefault() ?? new Point{X = -1, Y = -1};
    }
    
    public override string ToString()
    {
        if (print != "") return print;
        var size = field.GetLength(0);
        Func<int, int, Func<Point, string>, string> str = (x, y, funk) => Rectangle.Enumer(x, y).Select(a => funk(a)).Aggregate((a, b) => a + b);
        Func<Point, int, int[,], string> str2 = (a, i, field) => String.Format("{0, 3} |", (field[a.X, i] == 0 ? " " : field[a.X, i].ToString()));
        print   += str(size * 5, 1, a => "-") + "-\n|" 
                + str(field.GetLength(1), 1, b => str(size, 1, a => str2(a, b.X, field)) + "\n" 
                + str(size * 5, 1, a => "-") + "-\n|");
        print = print.Remove(print.Length - 1);
        return print;
    }
    
    public override int GetHashCode() => hashCode;
    
    public override bool Equals(Object obj)
    {
         if (obj == null || !this.GetType().Equals(obj.GetType()) || !(obj is Game game)) return false;
         return Rectangle.Enumer(field.GetLength(0), field.GetLength(1)).All(a => game.field[a.X, a.Y] == this.field[a.X, a.Y]);
    }
    
    public Game Move (int dx, int dy)
    {
        var x = zeroPoint.X + dx; var y = zeroPoint.Y + dy;
        if (dx * dx + dy * dy != 1) return null;
        if (x >= field.GetLength(0) || x < 0 || y >= field.GetLength(1) || y < 0) return null;
        var newGame = (int[,]) field.Clone();
        newGame[x, y] = 0; newGame[zeroPoint.X, zeroPoint.Y] = field[x, y];
        return new Game(newGame);
    }
    
    public IEnumerable<Game> AllMove() => Rectangle.Enumer(3, 3).Select(a => Move(a.X - 1, a.Y - 1)).Where(a => a != null);
}

class Rectangle
{
    public static IEnumerable<Point> Enumer(int sizeX, int sizeY)
    {
        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
                yield return new Point{X = x, Y = y};
    }
}

public class GameRezult
{
    public List<Game> trueTrack;
    public int time;
    public int step { get => trueTrack.Count - 1; }
    public bool finish;
    public int count;
 
    public GameRezult()
    {
        finish = false;
        trueTrack = new List<Game>();
    }
        
    public void RunningMultipleThreads(Game startGame, Game targetGame)
    {
        var watch = new Stopwatch();
        watch.Start();
        var trackNew = new Dictionary<Game, Game>(); bool flag = false;
        flag = startGame.AllMove().Where(d => !trackNew.ContainsKey(d)).Any(d => {trackNew.Add(d, null); return d.Equals(targetGame);});
        if (flag)
        {
            trueTrack.Add(startGame);
            trueTrack.Add(targetGame);
            watch.Stop();
            time = (int)watch.ElapsedMilliseconds;
            finish = true; return;
        }
        var findList = new List<GameRezult>();
        foreach (var d in trackNew) 
        {
            var v = new GameRezult();
            var track = new Dictionary<Game, Game>(trackNew);
            track.Remove(d.Key);
            Action find = () => v.FindRezult(d.Key, targetGame, track);
            Task.Run(find);
            findList.Add(v);
        }
        bool flag2 = true; bool flag3 = false;
        while (flag2 && !flag3)
        {
            Thread.Sleep(200); flag2 = false; count = 0;
            foreach (var g in findList)
            {
                count += g.count;
                if (!g.finish) flag2 = true;
                if (g.finish && g.trueTrack != null) 
                {
                    flag3 = true;
                    trueTrack.Add(startGame);
                    trueTrack.AddRange(g.trueTrack);
                }
            }
        }
        watch.Stop();
        time = (int)watch.ElapsedMilliseconds;
        finish = true; return;
    }
    
    public void FindRezult(Game startGame, Game targetGame, Dictionary<Game, Game> track)
    {
        var qu = new Queue<Game>(); bool flag = false;
        track.Add(startGame, null); qu.Enqueue(startGame);
        if (startGame == targetGame) flag = true;
        
        while (qu.Count > 0 && !flag && !finish)
        {
            var game = qu.Dequeue();
            flag = game.AllMove().Where(d => !track.ContainsKey(d)).Any(d => {qu.Enqueue(d); track.Add(d, game); count++; return d.Equals(targetGame);});
        }
        if (flag)
            while (targetGame != null)
            {
                trueTrack.Add(targetGame);
                targetGame = track[targetGame];
            }
        trueTrack.Reverse();
        finish = true;
    }
}

class GetGame
{
    public static Game GetTargetGame(int sizeX, int sizeY)
    {
        var fieldGame = new int[sizeX, sizeY]; int k = 1;
        Rectangle.Enumer(sizeX, sizeY).All(p => {fieldGame[p.X, p.Y] = k; k++; return true;});
        fieldGame[sizeX - 1, sizeY - 1] = 0;
        return new Game(fieldGame);
    }
    
    public static Game CreateGame(Game startGame, int countStep)
    {
        var targetGame = new Game((int[,])startGame.field.Clone());
        var rand = new Random();
        Game newGame = null;
        while (countStep > 0)
        {
            int step = rand.Next(0, 4);
            switch(step)
            {
                case 0: newGame = targetGame.Move(0, 1); break;
                case 1: newGame = targetGame.Move(1, 0); break;
                case 2: newGame = targetGame.Move(0, -1); break;
                case 3: newGame = targetGame.Move(-1, 0); break;
            } 
            
            if (newGame == null) continue; 
            targetGame = newGame;
            countStep--;
        }
        return targetGame;
    }
}

class Ticker
{
    private int spaceSize;
    private int k = 0;
    public bool finish;
    private double time = 0;
    private GameRezult rezult;
    public int period = 200;

    public Ticker(GameRezult rezult, int spaceSize)
    {
        this.spaceSize = spaceSize;
        finish = false;
        this.rezult = rezult;
    }
    
    public string Tic()
    {
        string currentStr = "";
        for (int i = 0; i < spaceSize; i++)
            currentStr += i == k ? "_" : " ";
        k = k < spaceSize ? k + 1 : 0;
        return currentStr;
    }
    
    public void TicStart()
    {
        finish = false;
        var timer = new System.Timers.Timer(period);
        timer.Elapsed += ( sender, e ) => 
        {
            Console.Clear();
            Console.WriteLine("Решение" + Tic() + " {0:f2} c.", time);
            Console.WriteLine("Рассмотрено {0:#,#} вариантов", rezult.count);
            time = time + (double)period/1000;
        };
        timer.Start();
        while (!finish) Thread.Sleep(period);
        timer.Stop();
        timer.Dispose();
    }
}

class Program
{
    static void Main() 
    {
        Console.Write("Введите размерность по Х: ");
        var sizeX = Int32.Parse(Console.ReadLine());
        
        Console.Write("Введите размерность по Y: ");
        var sizeY = Int32.Parse(Console.ReadLine());
        
        Console.Write("Введите сложность: ");
        var n = Int32.Parse(Console.ReadLine());

        var targetGame = GetGame.GetTargetGame(sizeX, sizeY);
        var startGame = GetGame.CreateGame(targetGame, n);
        
        Console.Write(startGame);
        Console.ReadKey();

        var rezult = new GameRezult();
        var str = new Ticker(rezult, 5);
        Task.Run(str.TicStart);
        //Action find = () => rezult.FindRezult(startGame, targetGame, new Dictionary<Game, Game>());
        Action find = () => rezult.RunningMultipleThreads(startGame, targetGame);
        Task.Run(find);
        while (!rezult.finish) Thread.Sleep(200);
        str.finish = true;
        
        Thread.Sleep(400);
        Console.WriteLine("{0} ", rezult.step > 0 ? "Успешно!": "НЕ УСПЕШНО ");
        Console.WriteLine($"Step: {rezult.step} Time: {rezult.time}ms");
        foreach (var s in rezult.trueTrack)
            {
                Console.ReadKey();
                Console.Clear();
                Console.WriteLine("РЕШЕНИЕ");
                Console.WriteLine(s);
            }
        Console.WriteLine($"Step: {rezult.step} Time: {rezult.time}ms");
    }
}
