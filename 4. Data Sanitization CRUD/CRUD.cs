global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;

using Email_Automation_Update.Supplemental.Methods;
using OrchestraInformation;


namespace SqlData
{
    public class Program
    {
        public static bool ContinueCrud { get; set; } = true; 
        public static bool CanConnect { get; set; }

        public static void CrudOperationSelection()
        {

            while (ContinueCrud)
            {
                Console.WriteLine(LoadingAndDisplay.divider);

                Console.WriteLine("\nSelect what you would like to do to your data:");
                Console.WriteLine("\t1. Create");
                Console.WriteLine("\t2. Read");
                Console.WriteLine("\t3. Update");
                Console.WriteLine("\t4. Delete");
                Console.WriteLine("\t5. Exit editing");

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("Selection => ");

                int.TryParse(Console.ReadLine(), out int choice);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;

                Action crudOperation = CrudOperation(choice);
                crudOperation.Invoke();
            }
        }

        public static Action CrudOperation(int choice)
        {
            return choice switch
            {
                1 => Create,
                2 => Read,
                3 => Update,
                4 => Delete,
                5 => Exit,
                _ => () => Console.WriteLine("Not an option")
            };
        }

        public static async void Create()
        {
            using (OrchestraRecordContext context = new OrchestraRecordContext())
            {
                if (CanConnect = ConnectionTest(context))
                {
                    OrchestraRecord newRecord = new OrchestraRecord
                    {
                        State = InputOrchestraInfo("state"),
                        County = InputOrchestraInfo("county"),
                        OrchestraName = InputOrchestraInfo("orchestra name"),
                        Website = InputOrchestraInfo("orchestra website"),
                        Email = InputOrchestraInfo("orchestra email")
                    };

                    context.OrchestraInformation.Add(newRecord);

                    try
                    {
                        context.SaveChanges();
                        Console.WriteLine("Record saved!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Record not saved ({e.Message})");
                    }
                }
                else
                {
                    Console.WriteLine("Could not connect to database");
                }
            }
        }

        public static void Read()
        {
            using (var context = new OrchestraRecordContext())
            {
                if (context.OrchestraInformation.Count() > 0)
                {
                    foreach (var record in context.OrchestraInformation.OrderBy(record => record.County))
                    {
                        if (!(record.Website.Length < 40))
                        {
                            record.Website = record.Website.Substring(0, 40) + "...";
                        }
                        Console.WriteLine(record);
                    }
                }
                else
                {
                    Console.WriteLine("No records");
                }
            }
        }

        public static void Update() 
        {
            using (var context = new OrchestraRecordContext())
            {
                string number = "";

                Console.Write("Enter log number to update: ");
                if (int.TryParse(Console.ReadLine(), out int result)) { }

                var record = context.OrchestraInformation.Find(result);

                if (record != null)
                {
                    Console.WriteLine("Update which part of the record: ");
                    Console.WriteLine("1. State");
                    Console.WriteLine("2. County");
                    Console.WriteLine("3. Orchestra name");
                    Console.WriteLine("4. Website");
                    Console.WriteLine("5. Email");

                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write("Selection => ");

                    int.TryParse(Console.ReadLine(), out int choice);
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;

                    var _ = InputOrchestraInfo(choice, record);
                    context.SaveChanges();
                    Read();
                }
                else
                {
                    Console.WriteLine("Invalid record");
                }
            }
        }

        public static void Delete()
        {
            using (var context = new OrchestraRecordContext())
            {
                Console.Write("Enter record to delete: ");
                if (int.TryParse(Console.ReadLine(), out int result)) { }

                var record = context.OrchestraInformation.Find(result);
                if (record != null)
                {
                    try
                    {
                        context.OrchestraInformation.Remove(record);
                        Console.WriteLine("Record removed");
                        context.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to delete the entry");
                        Console.WriteLine($"Error message: {e.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("No record to delete");
                }
            }

        }

        public static void Exit()
        {
            ContinueCrud = false;
        }

        public static string InputOrchestraInfo(params object[] values)
        {
            string response = "";

            using (OrchestraRecordContext context = new OrchestraRecordContext())
            {
                if (CanConnect = ConnectionTest(context))
                {
                    if (values.Count() > 1)
                    {
                        if (values[1] is OrchestraRecord currentRecord)
                        {
                            switch (values[0])
                            {
                                case 1:
                                    currentRecord.State = InputOrchestraInfo("state");
                                    Console.WriteLine("State updated!");
                                    break;
                                case 2:
                                    currentRecord.County = InputOrchestraInfo("county");
                                    Console.WriteLine("County updated!");
                                    break;
                                case 3:
                                    currentRecord.OrchestraName = InputOrchestraInfo("orchestra name");
                                    Console.WriteLine("Orchestra name updated!");
                                    break;
                                case 4:
                                    currentRecord.Website = InputOrchestraInfo("website");
                                    Console.WriteLine("Website updated!");
                                    break;
                                case 5:
                                    currentRecord.Email = InputOrchestraInfo("email");
                                    Console.WriteLine("Email updated!");
                                    break;
                                default:
                                    Console.WriteLine("Invalid selection");
                                    break;
                            }
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        Console.Write($"Enter {values[0]}: ");
                        response = Console.ReadLine();
                        return response;
                    }
                }
            }
            return "";
        }

        public static bool ConnectionTest(OrchestraRecordContext context)
        {
            return context.Database.CanConnect() ? true : false;
        }
    }


    public class OrchestraRecordContext : DbContext
    {
        public OrchestraRecordContext() : base() { }

        //These map to the tables in the database, where the table name is the same as the DbSet property name
        //The <T> is the custom class that maps to the table, where the properties in the class map to the columns in the table
        public DbSet<OrchestraRecord> OrchestraInformation { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("");
        }
    }
}