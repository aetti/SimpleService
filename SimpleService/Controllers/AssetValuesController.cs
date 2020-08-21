using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleService.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SimpleService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AssetValuesController : ControllerBase
    {
        private EFDatabaseContext context;
        private List<Asset> AssetList;
        private string curFile = @"Assets.xml";
        private string lastUpdateTime = @"LastUpdateTime.txt";
        private string firstRun = @"FirstRun.txt";
        public AssetValuesController(EFDatabaseContext ctx) => context = ctx;

        [HttpGet]
        public void UpdateAssetsFromFile()
        {
            if (!System.IO.File.Exists(curFile))
            Serialize();

            Deserialize();

            Console.WriteLine("\nLast update time is: " + GetLastUpdateTime().ToString());

            if(ReadFirstRun())
            {
                setDefaultValuesToDb();
            }
                
            var assetListWithGreaterDates = listOfAssetsWithDateGreaterThanLastUpdateDate(AssetList);
            WriteToConsole(assetListWithGreaterDates, "List of assets from the file with the dates greater than the last update date");

            var assetListWithoutDoubles = listOfAssetsFromFileWithoutDoubles(assetListWithGreaterDates);
            WriteToConsole(assetListWithoutDoubles, "List of assets from the file without doubles, only assets with the newer timestamp are taken");

            if (!ReadFirstRun())
            {
                try
                {
                    Console.WriteLine("Start saving assets to the database...");
                    UpdateDB(assetListWithoutDoubles);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to update the database. Reason: " + e.Message);
                    throw;
                }
            }

            SetFirstRunToFalse();
        }

        [HttpGet("{id1}/{id2}")]
        public object GetAsset(string id1,bool id2)
        {          
            switch (id1)
            {
                case "IsCash":
                    return context.Assets.Where(p => p.IsCash == id2).Select(p => p.Id).ToList();
                case "IsFixIncome":
                    return context.Assets.Where(p => p.IsFixIncome == id2).Select(p => p.Id).ToList();
                case "IsConvertible":
                    return context.Assets.Where(p => p.IsConvertible == id2).Select(p => p.Id).ToList();
                case "IsSwap":
                    return context.Assets.Where(p => p.IsSwap == id2).Select(p => p.Id).ToList();
                case "IsFuture":
                    return context.Assets.Where(p => p.IsFuture == id2).Select(p => p.Id).ToList();
                default:
                    return context.Assets.Where(p => p.IsCash == id2).Select(p => p.Id).ToList();
            }           
        }

        [HttpPut("{id1}/{id2}/{id3}/{id4}")]
        public void UpdateAsset(int id1, string id2, bool id3, DateTime id4)
        {
            DateTime TimeNow;

            if (id4 != null)
                TimeNow = id4;
            else
                TimeNow = DateTime.Now;

            var UpdatedAsset = context.Assets.AsNoTracking().FirstOrDefault(p => p.Id == id1);
            
            Asset asset = new Asset();

            asset.Id = id1;
            asset.Name = UpdatedAsset.Name;

            switch (id2)
            {
                case "IsCash":
                    asset.IsCash = id3;
                    break;
                case "IsFixIncome":
                    asset.IsFixIncome = id3;
                    break;
                case "IsConvertible":
                    asset.IsConvertible = id3;
                    break;
                case "IsSwap":
                    asset.IsSwap = id3;
                    break;
                case "IsFuture":
                    asset.IsFuture = id3;
                    break;
            }

            asset.TimeStamp = TimeNow;

            if (UpdatedAsset.TimeStamp < asset.TimeStamp)
            {
                context.Assets.Update(asset);
                context.SaveChanges();
            }          
        }

        void Serialize()
        {
            AssetList = context.Assets.Select(p => p).ToList();

            FileStream fs = new FileStream(curFile, FileMode.Create);

            try
            {
                Console.WriteLine("\nSerialization to the file is started");
                XmlSerializer formatter = new XmlSerializer(typeof(List<Asset>));
                //BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, AssetList);
                Console.WriteLine("Done");

            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        void Deserialize()
        {
            AssetList = null;

            FileStream fs = new FileStream(curFile, FileMode.Open);
            try
            {
                Console.WriteLine("\nDeserialization from the file is started");
                XmlSerializer formatter = new XmlSerializer(typeof(List<Asset>));
                //BinaryFormatter formatter = new BinaryFormatter();
                AssetList = (List<Asset>)formatter.Deserialize(fs);
                Console.WriteLine("Done");
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }

            //Console.WriteLine("\nList of Assets");
            WriteToConsole(AssetList, "List of Assets from the file");
        }

        void WriteToConsole(List<Asset> xAssetList, string message)
        {
            if (xAssetList != null)
            {
                Console.WriteLine("\n" + message);

                foreach (var x in xAssetList)
                {
                    Console.WriteLine(x.Id);
                    Console.WriteLine(x.Name);
                    Console.WriteLine(x.IsCash);
                    Console.WriteLine(x.IsConvertible);
                    Console.WriteLine(x.IsFixIncome);
                    Console.WriteLine(x.IsFuture);
                    Console.WriteLine(x.IsSwap);
                    Console.WriteLine(x.TimeStamp);
                    Console.WriteLine(" ");
                }
            }
        }

        void SetLastUpdateTime(DateTime dt)
        {
            System.IO.File.WriteAllText(lastUpdateTime, dt.ToString());
        }

        DateTime GetLastUpdateTime()
        {
            string text = System.IO.File.ReadAllText(lastUpdateTime);
            var parsedDate = DateTime.Parse(text);
            return parsedDate;
        }

        bool ReadFirstRun()
        {
            string text = System.IO.File.ReadAllText(firstRun);
            return System.Convert.ToBoolean(text);
        }

        void SetFirstRunToFalse()
        {
            System.IO.File.WriteAllText(firstRun, "FALSE");
        }

        void UpdateDB(List<Asset> assetListSaveToDb)
        {         
            foreach (var a in assetListSaveToDb)
            {
                var UpdatedAsset = context.Assets.AsNoTracking().FirstOrDefault(p => p.Id == a.Id);

                if (UpdatedAsset != null)
                {
                    Asset asset = new Asset();

                    asset.Id = a.Id;
                    asset.Name = UpdatedAsset.Name;
                    asset.IsCash = a.IsCash;
                    asset.IsConvertible = a.IsConvertible;
                    asset.IsFixIncome = a.IsFixIncome;
                    asset.IsFuture = a.IsFuture;
                    asset.IsSwap = a.IsSwap;
                    asset.TimeStamp = a.TimeStamp;

                    context.Assets.Update(asset);
                    context.SaveChanges();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Asset with the Id={0} has been saved to the database", a.Id);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Asset with the Id={0} is not present in the database", a.Id);                    
                }

                    Console.ForegroundColor = ConsoleColor.White;
            }

            SetLastUpdateTime(DateTime.Now);
        }

        List<Asset> listOfAssetsWithDateGreaterThanLastUpdateDate(List<Asset> assetListFromFile)
        {
            List<Asset> assetListWithGreaterDate = new List<Asset>();
            DateTime t = GetLastUpdateTime();

            foreach (var a in assetListFromFile)
            {
                if (a.TimeStamp > t)
                {
                    assetListWithGreaterDate.Add(a);
                }
            }

            return assetListWithGreaterDate;
        }

        List<Asset> listOfAssetsPresentInFileButAbsentInDb(List<Asset> assetListFromFile, List<Asset> assetListFromDB)
        {
            List<int> absentAssetIdsInDB = new List<int>();
            List<Asset> assetListPresentInFileAbsentInDb = new List<Asset>();

            var assetIdsFromFile = assetListFromFile.Select(p => p.Id).ToList();
            var assetIdsFromDB = assetListFromDB.Select(p => p.Id).ToList();
           
            foreach (var a in assetIdsFromFile)
            {
                if(!assetIdsFromDB.Contains(a))
                {
                    absentAssetIdsInDB.Add(a);
                    Console.WriteLine("\nAsset with the Id={0} is present in the file, but is not present in DB", a);
                }                
            }
                   
            foreach (var a in assetListFromFile)
            {
                if (absentAssetIdsInDB.Contains(a.Id))
                {
                    assetListPresentInFileAbsentInDb.Add(a);
                }
            }

            return assetListPresentInFileAbsentInDb;
        }

        void setDefaultValuesToDb()
        {
            Console.WriteLine("Set FALSE to properties and DateTime MinValue for the TimeStamp property");

            List<Asset> assetListFromDB = context.Assets.AsNoTracking().Select(p => p).ToList();

            assetListFromDB = context.Assets.AsNoTracking().Select(p => p).ToList();

            List<Asset> tempList = assetListFromDB;

            foreach (var a in tempList)
            {
                a.IsCash = false;
                a.IsConvertible = false;
                a.IsFixIncome = false;
                a.IsFuture = false;
                a.IsSwap = false;
                a.TimeStamp = DateTime.MinValue;
            }

            foreach (var a in tempList)
            {
                var UpdatedAsset = context.Assets.AsNoTracking().FirstOrDefault(p => p.Id == a.Id);

                if (UpdatedAsset != null)
                {
                    Asset asset = new Asset();

                    asset.Id = a.Id;
                    asset.Name = UpdatedAsset.Name;
                    asset.IsCash = a.IsCash;
                    asset.IsConvertible = a.IsConvertible;
                    asset.IsFixIncome = a.IsFixIncome;
                    asset.IsFuture = a.IsFuture;
                    asset.IsSwap = a.IsSwap;
                    asset.TimeStamp = a.TimeStamp;

                    context.Assets.Update(asset);
                    context.SaveChanges();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Asset with the Id={0} has been saved to the database", a.Id);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Asset with the Id={0} is not present in the database", a.Id);
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        List<Asset> listOfAssetsFromFileWithoutDoubles(List<Asset> assetListFromFile)
        {
            List<Asset> assetListWithoutDoubles = new List<Asset>();
            List<int> excludList = new List<int>();

            foreach (var x in assetListFromFile)
            {
                if (!excludList.Contains(x.Id))
                {
                    Asset compliantAsset;
                    var assetsWithSameID = (from y in assetListFromFile where y.Id == x.Id orderby y.TimeStamp descending select y).ToList();

                    if (assetsWithSameID.Count >= 1)
                    {
                        compliantAsset = assetsWithSameID.First();
                        assetListWithoutDoubles.Add(compliantAsset);
                        excludList.Add(x.Id);
                    }

                }
            }

            return assetListWithoutDoubles;

        }
    }
}
