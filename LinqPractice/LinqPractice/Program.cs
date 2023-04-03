using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace linqTest
{
    [XmlRoot(ElementName = "engine")]
    public class Engine
    {
        public double displacement;
        public double horsePower;
        [XmlAttribute]
        public string model;
        public Engine() { }
        public Engine(double displacement, double horsePower, string model)
        {
            this.displacement = displacement;
            this.horsePower = horsePower;
            this.model = model;
        }
    }

    [XmlType("car")]
    public class Car
    {
        public string model;
        public int year;
        [XmlElement(ElementName = "engine")]
        public Engine motor;

        public Car() { }
        public Car(string model, Engine engine, int year)
        {
            this.model = model;
            this.year = year;
            this.motor = engine;
        }
    }


    public class Program
    {

        private static void ProjectionAndGrouping(List<Car> myCars)
        {
            var anon = from car in myCars
                       where car.model == "A6"
                       select new
                       {
                           hppl = car.motor.horsePower / car.motor.displacement,
                           engineType = String.Compare(car.motor.model, "TDI") == 0
                           ? "diesel"
                           : "petrol",
                       };
            foreach (var a in anon)
            {
                Console.WriteLine("engine: {0} hppl: {1}", a.engineType, a.hppl);
            }
            IEnumerable<IGrouping<string, double>> results =
            from a in anon
            group a.hppl
            by a.engineType;
            foreach (IGrouping<string, double> group in results)
            {
                double med = 0;
                int count = 0;
                foreach (double value in group)
                {
                    med += value;
                    count++;
                }
                med = med / count;
                Console.WriteLine("{0}: {1}", group.Key, med);
            }
        }

        private static void Serialization(List<Car> myCars, string path)
        {
            using (TextWriter filestream = new StreamWriter(path))
            {
                XmlSerializer x = new(myCars.GetType(), new XmlRootAttribute("cars"));
                x.Serialize(filestream, myCars);
            }
        }
        private static List<Car>? Deserialization(string path)
        {
            using (Stream reader = new FileStream(path, FileMode.Open))
            {
                XmlSerializer x = new XmlSerializer(typeof(List<Car>), new XmlRootAttribute("cars"));
                var output = x.Deserialize(reader);
                return output as List<Car>;
            }
        }

        private static double XpathAvgHP(string path)
        {
            XElement rootNode = XElement.Load(path);
            double avgHP = (double)rootNode.XPathEvaluate("sum(/car/engine[@model!='TDI']/horsePower) div count(/car/engine[@model!='TDI'])");
            return avgHP;
        }

        private static void createXMLNoDuplicates(string path, string newName)
        {
            XElement rootNode = XElement.Load(path);
            IEnumerable<XElement> models = rootNode.XPathSelectElements("/car[not(model=preceding::car/model)]");
            var currentDirectory = Directory.GetCurrentDirectory();
            string xmlFileLocation = Path.Combine(currentDirectory, newName + ".xml");
            XElement newRoot = new XElement("cars", models);
            newRoot.Save(xmlFileLocation);
        }

        private static void createXHTMLtable(List<Car> myCars)
        {
            var filename = "template.html";
            var currentDirectory = Directory.GetCurrentDirectory();
            string htmlFileLocation = Path.Combine(currentDirectory, filename);

            IEnumerable<XElement> tableNodes = myCars.Select(n =>
                new XElement("tr", new XAttribute("style", "border: 1px black solid"),
                        new XElement("td", new XAttribute("style", "border: 1px black solid"), n.model),
                        new XElement("td", new XAttribute("style", "border: 1px black solid"), n.motor.model),
                        new XElement("td", new XAttribute("style", "border: 1px black solid"), n.motor.displacement),
                        new XElement("td", new XAttribute("style", "border: 1px black solid"), n.motor.horsePower),
                        new XElement("td", new XAttribute("style", "border: 1px black solid"), n.year)
                        ));
            XElement table = new XElement("table", new XAttribute("style", "border: 1px black solid"), tableNodes);
            XElement bod = new XElement("body", table);
            XDocument result = new XDocument(bod);
            result.Save(htmlFileLocation);
        }

        private static void createXmlFromLinq(List<Car> myCars)
        {
            IEnumerable<XElement> nodes = myCars.Select(n =>
                new XElement("car",
                    new XElement("model", n.model),
                    new XElement("year", n.year),
                    new XElement("engine", new XAttribute("model", n.motor.model),
                        new XElement("displacement", n.motor.displacement),
                        new XElement("horsePower", n.motor.horsePower))
                    ));

            XElement rootNode = new XElement("cars", nodes,
                new XAttribute("xmlns-xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute("xmlns-xsd", "http://www.w3.org/2001/XMLSchema")); //create a root node to contain the query results
            var currentDirectory = Directory.GetCurrentDirectory();
            var filename = "CarsFromLinq.xml";
            string xmlFileLocation = Path.Combine(currentDirectory, filename);
            rootNode.Save(xmlFileLocation);

        }

        private static void ModifyXML(string path)
        {
            XDocument doc = XDocument.Load(path);
            foreach (XElement cars in doc.Elements())
            {
                foreach (XElement car in cars.Elements())
                {
                    string year = car.Element("year").Value;
                    car.Element("year").Remove();
                    car.Element("engine").Element("horsePower").Name = "hp";
                    car.Element("model").Add(new XAttribute("year", year));
                }
            }
            doc.Save(path);
        }

        private static void Main(string[] args)
        {
            var filename = "CarsCollection.xml";
            var currentDirectory = Directory.GetCurrentDirectory();
            string xmlFileLocation = Path.Combine(currentDirectory, filename);
            List<Car> myCars = new List<Car>(){
                new Car("E250", new Engine(1.8, 204, "CGI"), 2009),
                new Car("E350", new Engine(3.5, 292, "CGI"), 2009),
                new Car("A6", new Engine(2.5, 187, "FSI"), 2012),
                new Car("A6", new Engine(2.8, 220, "FSI"), 2012),
                new Car("A6", new Engine(3.0, 295, "TFSI"), 2012),
                new Car("A6", new Engine(2.0, 175, "TDI"), 2011),
                new Car("A6", new Engine(3.0, 309, "TDI"), 2011),
                new Car("S6", new Engine(4.0, 414, "TFSI"), 2012),
                new Car("S8", new Engine(4.0, 513, "TFSI"), 2012)
            };

            ProjectionAndGrouping(myCars);
            Serialization(myCars, xmlFileLocation);
            List<Car> newCars = new List<Car>();
            newCars = Deserialization(xmlFileLocation);
            if (newCars != null)
            {
                ProjectionAndGrouping(newCars);
            }
            double scr = XpathAvgHP(xmlFileLocation);
            Console.WriteLine("avg HP: {0}", scr);

            createXMLNoDuplicates(xmlFileLocation, "CarsCollectionNoDuplicates");
            createXmlFromLinq(myCars);
            createXHTMLtable(myCars);

            ModifyXML(xmlFileLocation);
        }
    }
}