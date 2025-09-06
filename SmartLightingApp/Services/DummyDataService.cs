using SmartLightingApp.Models;

namespace SmartLightingApp.Services;

public class DummyDataService
    {
        public static List<RelayBoard> GetDummyBoards()
        {
            return new List<RelayBoard>
            {
                new RelayBoard
                {
                    Id = 1,
                    Name = "Salon 8CH Relay",
                    IpAddress = "10.0.4.39",
                    IsOnline = true,
                    LastSeen = DateTime.Now.AddMinutes(-2),
                    Relays = new List<RelayItem>
                    {
                        new RelayItem(1, "Salon Ana Işık") { IsOn = true, HasTimer = true },
                        new RelayItem(2, "Salon Spot") { IsOn = true, HasTimer = false },
                        new RelayItem(3, "TV Ünitesi LED") { IsOn = false, HasTimer = false },
                        new RelayItem(4, "Balkon Kapısı") { IsOn = true, HasTimer = true },
                        new RelayItem(5, "Yemek Odası") { IsOn = true, HasTimer = false },
                        new RelayItem(6, "Relay 6") { IsOn = false, HasTimer = false },
                        new RelayItem(7, "Relay 7") { IsOn = false, HasTimer = false },
                        new RelayItem(8, "Relay 8") { IsOn = false, HasTimer = false }
                    }
                },
                new RelayBoard
                {
                    Id = 2,
                    Name = "Mutfak 4CH Relay",
                    IpAddress = "192.168.1.102",
                    IsOnline = true,
                    LastSeen = DateTime.Now.AddMinutes(-1),
                    Relays = new List<RelayItem>
                    {
                        new RelayItem(1, "Mutfak Ana Işık") { IsOn = true, HasTimer = true },
                        new RelayItem(2, "Tezgah Altı LED") { IsOn = true, HasTimer = false },
                        new RelayItem(3, "Fırın Davlumbazı") { IsOn = false, HasTimer = false },
                        new RelayItem(4, "Kahve Makinesi") { IsOn = false, HasTimer = true }
                    }
                },
                new RelayBoard
                {
                    Id = 3,
                    Name = "Yatak Odası 2CH",
                    IpAddress = "192.168.1.103",
                    IsOnline = false,
                    LastSeen = DateTime.Now.AddHours(-2),
                    Relays = new List<RelayItem>
                    {
                        new RelayItem(1, "Yatak Odası Ana") { IsOn = false, HasTimer = false },
                        new RelayItem(2, "Abajur") { IsOn = false, HasTimer = false }
                    }
                }
            };
        }
    }