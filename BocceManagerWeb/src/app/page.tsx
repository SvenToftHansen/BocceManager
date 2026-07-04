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
        <div className="relative z-10 flex flex-col items-center gap-[2vh] px-[4vw] text-center text-white">
          <h1 className="text-[clamp(1.75rem,6vw,4.5rem)] font-semibold tracking-tight">
            Golden Vista Bocce League
          </h1>
          <p className="max-w-xl text-[clamp(1rem,2.2vw,1.5rem)] text-zinc-200">
            Standings, schedules, and league info for players and fans.
          </p>
        </div>
      </section>

      <section className="mx-auto w-full max-w-4xl flex-1 p-[clamp(1.5rem,4vw,4rem)]">
        <h2 className="mb-[clamp(1.5rem,3vw,2.5rem)] text-center text-[clamp(1.25rem,3vw,2rem)] font-semibold tracking-tight text-foreground">
          Current Seasons
        </h2>

        {currentSeasons.length === 0 ? (
          <p className="text-center text-muted-foreground">
            No current seasons are underway right now. Check back soon.
          </p>
        ) : (
          <div
            className={
              currentSeasons.length === 1
                ? "flex justify-center"
                : "grid gap-[clamp(1rem,3vw,1.5rem)] sm:grid-cols-2"
            }
          >
            {currentSeasons.map(({ league, season }) => (
              <div
                key={season.Id}
                className="group w-full max-w-sm cursor-pointer select-none rounded-xl bg-gray-200 p-[clamp(1rem,2.5vw,1.5rem)] text-black shadow-inner ring-1 ring-black/10 transition-colors duration-150 hover:bg-green-200 active:bg-green-900"
              >
                <h3 className="text-[clamp(1.1rem,2vw,1.4rem)] font-semibold group-active:text-white">
                  {league.Name}
                </h3>
                <p className="mt-1 text-[clamp(0.9rem,1.5vw,1.1rem)] text-gray-700 group-active:text-white">
                  {season.Name}
                </p>
                <p className="mt-[clamp(0.75rem,1.5vw,1rem)] text-[clamp(0.8rem,1.2vw,0.9rem)] text-gray-600 group-active:text-white">
                  Standings and schedules coming soon.
                </p>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
