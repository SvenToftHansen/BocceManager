import Image from "next/image";
import { prisma } from "@/lib/prisma";
import heroImage from "../../public/images/bocce-hero.jpg";

export const dynamic = "force-dynamic";

export default async function Home() {
  const leagues = await prisma.leagues.findMany({
    where: { IsActive: true, Seasons: { some: { IsCurrent: true } } },
    include: { Seasons: { where: { IsCurrent: true } } },
    orderBy: { Name: "asc" },
  });

  const currentSeasons = leagues.flatMap((league) =>
    league.Seasons.map((season) => ({ league, season }))
  );

  return (
    <div className="flex flex-1 flex-col">
      <section className="relative flex h-screen items-center justify-center overflow-hidden">
        <Image
          src={heroImage}
          alt="Bocce balls on the court"
          fill
          priority
          className="object-cover"
        />
        <div className="absolute inset-0 bg-black/55" />
        <div className="relative z-10 flex w-full max-w-4xl flex-col items-center gap-[1.5vh] px-[4vw] text-center text-white">
          <h1 className="text-[clamp(1.5rem,4.5vw,3.25rem)] font-semibold tracking-tight">
            Golden Vista Bocce League
          </h1>
          <p className="max-w-xl text-[clamp(0.9rem,2vw,1.25rem)] text-zinc-200">
            Standings, schedules, and league info for players and fans.
          </p>

          <div className="mt-[1.5vh] w-full rounded-2xl bg-black/50 p-[clamp(1rem,2.5vw,2rem)] ring-1 ring-white/20 backdrop-blur-sm">
            <h2 className="mb-[clamp(0.75rem,2vw,1.25rem)] text-[clamp(1.1rem,2.5vw,1.5rem)] font-semibold tracking-tight text-white">
              Current Seasons
            </h2>

            {currentSeasons.length === 0 ? (
              <p className="text-white/80">
                No current seasons are underway right now. Check back soon.
              </p>
            ) : (
              <div
                className={
                  currentSeasons.length === 1
                    ? "flex justify-center"
                    : "grid gap-[clamp(0.75rem,2vw,1.25rem)] sm:grid-cols-2"
                }
              >
                {currentSeasons.map(({ league, season }) => (
                  <div
                    key={season.Id}
                    className="group w-full max-w-sm cursor-pointer select-none rounded-xl bg-gray-200/50 p-[clamp(0.75rem,2vw,1.25rem)] text-black shadow-inner ring-1 ring-black/10 transition-colors duration-150 hover:bg-green-200/50 active:bg-green-900/50"
                  >
                    <h3 className="text-[clamp(1rem,1.8vw,1.25rem)] font-semibold group-active:text-white">
                      {league.Name}
                    </h3>
                    <p className="mt-1 text-[clamp(0.8rem,1.3vw,1rem)] text-gray-700 group-active:text-white">
                      {season.Name}
                    </p>
                    <p className="mt-[clamp(0.5rem,1.2vw,0.75rem)] text-[clamp(0.7rem,1.1vw,0.85rem)] text-gray-600 group-active:text-white">
                      Standings and schedules coming soon.
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </section>
    </div>
  );
}
