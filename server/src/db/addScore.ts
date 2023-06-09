import fs from 'fs'
import path from 'path'
import * as c from '../common'

/*
 world files are stored in files named `${(thousands place of score)} - ${(number of entries inside)}.json`
  with file names like: `1000 - 20.json`
*/
export default async function (
  dbPath: string,
  ip?: string,
  score?: number,
): Promise<number> {
  if (!ip || !score || dbPath.includes('undefined'))
    return 0

  const scoreDataToSave: Score = {
    ip,
    score,
  }

  return useIndividualFiles(dbPath, scoreDataToSave)
}

async function useIndividualFiles(
  dbPath: string,
  scoreData: Score,
): Promise<number> {
  if (
    !fs.existsSync(
      path.join('./', 'data', dbPath.split('/')[0]),
    )
  ) {
    fs.mkdirSync(
      path.join('./', 'data', dbPath.split('/')[0]),
      {
        recursive: true,
      },
    )
    c.log(`Created ./data/${dbPath} directory`)
  }

  let existingData: string | null = null
  if (
    !fs.existsSync(
      path.join('./', 'data', dbPath + '.json'),
    )
  ) {
    fs.writeFileSync(
      path.join('./', 'data', dbPath + '.json'),
      JSON.stringify([scoreData]),
    )
    c.log(`Created ./data/${dbPath}.json file`)
    existingData = '[]'
  }
  if (existingData === null)
    existingData = await fs.promises
      .readFile(
        path.join('./', 'data', dbPath + '.json'),
        'utf8',
      )
      .catch((err) => {
        c.log(err)
        return null
      })

  let scoresAboveInFile = 0
  if (existingData) {
    let existingDataParsed = JSON.parse(existingData)
    const existingRecord = existingDataParsed.find(
      (score: Score) => score.ip === scoreData.ip,
    )
    if (existingRecord) {
      c.log(
        'gray',
        `Found existing record for ${scoreData.ip}`,
      )
      if (existingRecord.score < scoreData.score) {
        c.log('gray', `Existing record is lower, updating`)
        existingRecord.score = scoreData.score
        existingDataParsed = existingDataParsed.sort(
          (a: Score, b: Score) => b.score - a.score,
        )
        fs.promises.writeFile(
          path.join('./', 'data', dbPath + '.json'),
          JSON.stringify(existingDataParsed),
        )
      } else {
        c.log('gray', `Existing record is higher`)
      }

      scoresAboveInFile = existingDataParsed.filter(
        (score: Score) => score.score > scoreData.score,
      ).length
      return scoresAboveInFile + 1
    }

    c.log(
      'gray',
      `No existing record for ${scoreData.ip}, creating`,
    )

    scoresAboveInFile = existingDataParsed.filter(
      (score: Score) => score.score > scoreData.score,
    ).length
    existingDataParsed.splice(
      scoresAboveInFile,
      0,
      scoreData,
    )
    fs.promises
      .writeFile(
        path.join('./', 'data', dbPath + '.json'),
        JSON.stringify(existingDataParsed),
      )
      .catch((err) => {
        c.log(err)
      })
  } else {
    fs.promises
      .writeFile(
        path.join('./', 'data', dbPath + '.json'),
        JSON.stringify([scoreData]),
      )
      .catch((err) => {
        c.log(err)
      })
  }

  return scoresAboveInFile + 1
}

async function useSplitFiles(
  dbPath: string,
  scoreData: Score,
): Promise<number> {
  if (!fs.existsSync(path.join('./', 'data', dbPath))) {
    fs.mkdirSync(path.join('./', 'data', dbPath), {
      recursive: true,
    })
    c.log(`Created ./data/${dbPath} directory`)
  }

  // get files in world directory
  const files = (
    await fs.promises
      .readdir(path.join('./', 'data', dbPath))
      .catch((err) => {
        c.log(err)
        return []
      })
  ).sort(
    // low to high
    (a, b) =>
      parseInt(a.split(' - ')[0]) -
      parseInt(b.split(' - ')[0]),
  )
  const scoreRoundedToThousands =
    Math.floor(scoreData.score / 1000) * 1000
  const filesBelowScore = files.filter(
    (file) =>
      parseInt(file.split(' - ')[0]) <
      scoreRoundedToThousands,
  )

  let scoresAboveInFile = 0
  let scoreFile = files.find(
    (file) =>
      parseInt(file.split(' - ')[0]) ===
      scoreRoundedToThousands,
  )
  if (!scoreFile) {
    c.log(
      'creating new score file',
      dbPath + '/' + `${scoreRoundedToThousands} - 1.json`,
    )
    scoreFile = `${scoreRoundedToThousands} - 1.json`
    await fs.promises
      .writeFile(
        path.join('./', 'data', dbPath, scoreFile),
        JSON.stringify([scoreData]),
      )
      .catch((err) => {
        c.log(err)
      })
  } else {
    c.log(
      'adding score to existing file',
      dbPath + '/' + scoreFile,
    )
    const scoreFileData = JSON.parse(
      (await fs.promises
        .readFile(
          path.join('./', 'data', dbPath, scoreFile),
          'utf8',
        )
        .catch((err) => {
          c.log(err)
          return '[]'
        })) || '[]',
    )
    let scoresAboveInFile = scoreFileData.findIndex(
      (sd: Score) => sd.score < scoreData.score,
    )
    if (scoresAboveInFile === -1)
      scoresAboveInFile = scoreFileData.length
    scoreFileData.splice(scoresAboveInFile, 0, scoreData)
    await fs.promises
      .writeFile(
        path.join(
          './',
          'data',
          dbPath,
          `${scoreRoundedToThousands} - ${scoreFileData.length}.json`,
        ),
        JSON.stringify(scoreFileData),
      )
      .catch((err) => {
        c.log(err)
      })
    await fs.promises
      .unlink(path.join('./', 'data', dbPath, scoreFile))
      .catch((err) => {
        c.log(err)
      })
  }

  const scoresInPreviousFiles = filesBelowScore.reduce(
    (total, file) =>
      total + parseInt(file.split(' - ')[1].split('.')[0]),
    0,
  )

  return scoresInPreviousFiles + scoresAboveInFile + 1
}
