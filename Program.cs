using BaiduBot;

Console.WriteLine("请输入1或者2操作：1、挖词 2、清洗 3、扫站");
var i = Console.ReadLine();
if (i == "1")
{
    await new Waci().Go();
}
else if (i == "2")
{
    await new Cleaner().Go();
} else
{
    await new Siter().Go();
}

